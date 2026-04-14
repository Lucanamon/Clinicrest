using api.Application.Exceptions;

namespace api.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message, detail) = MapException(ex);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiError(
            statusCode,
            message,
            _environment.IsDevelopment() ? detail ?? ex.StackTrace : null,
            context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static (int StatusCode, string Message, string? Detail) MapException(Exception ex) =>
        ex switch
        {
            BusinessException businessException => (StatusCodes.Status400BadRequest, businessException.Message, businessException.Detail),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message, argumentException.StackTrace),
            UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, unauthorizedAccessException.Message, unauthorizedAccessException.StackTrace),
            KeyNotFoundException keyNotFoundException => (StatusCodes.Status404NotFound, keyNotFoundException.Message, keyNotFoundException.StackTrace),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", ex.StackTrace)
        };
}
