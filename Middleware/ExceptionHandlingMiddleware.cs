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
                _logger.LogError(exception, "Une erreur est survenue pendant le traitement de la requête.");
                await WriteErrorAsync(context, exception);
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
                DbUpdateException => (StatusCodes.Status500InternalServerError, "Erreur lors de l'enregistrement des données."),
                _ => (StatusCodes.Status500InternalServerError, "Une erreur interne est survenue.")
            };

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
