namespace api.Application.AuditLogs;

public class AuditLogListItemDto
{
    public Guid Id { get; init; }

    /// <summary>Actor id from JWT (typically user id).</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Resolved username when <see cref="UserId"/> matches a user row.</summary>
    public string? ActorUsername { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityName { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public string? OldValues { get; init; }

    public string? NewValues { get; init; }

    public DateTime Timestamp { get; init; }
}
