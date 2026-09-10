using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using EcommerceAPI.Data;
using EcommerceAPI.Repositories;
using EcommerceAPI.Services;
using EcommerceAPI.Repositories;
using EcommerceAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== ADD DATABASE CONFIGURATION =====
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ===== ADD CONTROLLERS =====
builder.Services.AddControllers();
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
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// ===== ADD CORS (Allow frontend to communicate) =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== BUILD THE APP =====
var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseCors("AllowAll");  // Enable CORS
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();  // Map controller routes

app.Run();