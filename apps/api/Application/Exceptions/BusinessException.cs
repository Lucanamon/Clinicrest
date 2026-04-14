namespace api.Application.Exceptions;

public sealed class BusinessException(string message, string? detail = null) : Exception(message)
{
    public string? Detail { get; } = detail;
}
