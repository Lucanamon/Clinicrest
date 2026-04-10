namespace api.Application.Abstractions;

/// <summary>Resolved per request (JWT). Used for audit attribution.</summary>
public interface ICurrentUserAccessor
{
    /// <summary>Stable actor id for audit rows (typically JWT sub / user id), or a well-known system label.</summary>
    string GetAuditUserId();
}
