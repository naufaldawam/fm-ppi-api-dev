using System.Net;
using System.Text.Json;
using MyService.Application.DTOs;

namespace MyService.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, errorCode, message) = exception switch
            {
                UnauthorizedAccessException => (401, "ERR-SYS-001", "Unauthorized access"),
                ArgumentNullException => (400, "ERR-VAL-001", "Required field is missing"),
                ArgumentException => (400, "ERR-VAL-002", exception.Message),
                KeyNotFoundException => (404, "ERR-SYS-002", "Resource not found"),
                _ => (500, "ERR-SYS-000", "An unexpected error occurred")
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.ErrorResponse(errorCode, message)));
        }
    }
}
