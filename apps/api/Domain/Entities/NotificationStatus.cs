namespace api.Domain.Entities;

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Retrying,
    Cancelled
}
