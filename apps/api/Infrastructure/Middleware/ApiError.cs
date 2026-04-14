namespace api.Infrastructure.Middleware;

public sealed record ApiError(
    int StatusCode,
    string Message,
    string? Detail,
    string TraceId);
