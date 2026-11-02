using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EcommerceAPI.Data;
using EcommerceAPI.Repositories;
using EcommerceAPI.Services;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

// Logger minimal pour tracer un éventuel échec de démarrage avant la lecture de la configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ===== SERILOG : journalisation structurée =====
    // Console lisible en développement + fichiers JSON (un événement par ligne) dans logs/, un par jour, 14 jours conservés.
    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EcommerceAPI")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(new CompactJsonFormatter(), "logs/ecommerce-.json",
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

    // ===== ADD DATABASE CONFIGURATION =====
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<AuditSaveChangesInterceptor>();
    builder.Services.AddDbContext<EcommerceContext>((services, options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
               .AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>())
    );

    // ===== ADD CONTROLLERS =====
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key est obligatoire.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true
            };
        });

    // DI registrations
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<ICouponService, CouponService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IReviewService, ReviewService>();
    builder.Services.AddScoped<IWishlistService, WishlistService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<IReturnService, ReturnService>();    builder.Services.AddScoped<OrderDocumentService>();
    // Password reset (e-mail code)
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

    // ===== ADD CORS (Allow frontend to communicate) =====
    // Origines autorisées : section Cors:AllowedOrigins (en production, mettre l'URL du frontend)
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // ===== BUILD THE APP =====
    var app = builder.Build();

    // Compte administrateur statique (admin@gmail.com)
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            await AdminSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<EcommerceContext>());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Impossible de créer le compte administrateur par défaut");
        }
    }

    // Une ligne structurée par requête (méthode, chemin, statut, durée, utilisateur)
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnostics, http) =>
        {
            diagnostics.Set("UserId", http.User.FindFirstValue(ClaimTypes.NameIdentifier));
            diagnostics.Set("UserEmail", http.User.FindFirstValue(ClaimTypes.Name));
            diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        };
    });

    app.UseMiddleware<EcommerceAPI.Middleware.ExceptionHandlingMiddleware>();
    app.UseStatusCodePages(async statusCodeContext =>
    {
        var response = statusCodeContext.HttpContext.Response;
        if (response.ContentLength.HasValue || response.HasStarted) return;

        response.ContentType = "application/json";
        var message = response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => "Authentification requise.",
            StatusCodes.Status403Forbidden => "Accès interdit.",
            StatusCodes.Status404NotFound => "Ressource introuvable.",
            _ => "La requête n'a pas pu être traitée."
        };
        await response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = response.StatusCode,
            message,
            traceId = statusCodeContext.HttpContext.TraceIdentifier
        }));
    });

    // ===== USE MIDDLEWARE =====
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();  // Swagger UI for testing
        app.UseSwaggerUI();
    }

    // Product images uploaded by the admin are served from wwwroot/uploads (created if missing)
    var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
    Directory.CreateDirectory(uploadsRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
        RequestPath = "/uploads"
    });
    app.UseHttpsRedirection();
    app.UseCors("Frontend");  // Enable CORS
    app.UseAuthentication();
    app.UseAuthorization();

    // Tous les journaux écrits pendant une requête portent l'utilisateur connecté
    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty("UserId", context.User.FindFirstValue(ClaimTypes.NameIdentifier)))
        using (LogContext.PushProperty("UserEmail", context.User.FindFirstValue(ClaimTypes.Name)))
        {
            await next();
        }
    });

    app.MapControllers();  // Map controller routes

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "L'application s'est arrêtée au démarrage");
}
finally
{
    Log.CloseAndFlush();
}
