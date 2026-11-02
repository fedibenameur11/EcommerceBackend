using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var (statusCode, message) = Map(exception);

                // Erreurs attendues (validation, stock, introuvable) : avertissement ; le reste : vraie erreur avec la trace
                if (statusCode >= StatusCodes.Status500InternalServerError)
                    _logger.LogError(exception, "Erreur serveur sur {Method} {Path} ({StatusCode})",
                        context.Request.Method, context.Request.Path.Value, statusCode);
                else
                    _logger.LogWarning("Requête refusée sur {Method} {Path} ({StatusCode}) : {Reason}",
                        context.Request.Method, context.Request.Path.Value, statusCode, exception.Message);

                await WriteErrorAsync(context, statusCode, message);
            }
        }

        private static (int StatusCode, string Message) Map(Exception exception) => exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
            DbUpdateException => (StatusCodes.Status500InternalServerError, "Erreur lors de l'enregistrement des données."),
            _ => (StatusCodes.Status500InternalServerError, "Une erreur interne est survenue.")
        };

        private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted) return;
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = statusCode,
                message,
                traceId = context.TraceIdentifier
            }));
        }
    }
}
