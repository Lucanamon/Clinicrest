namespace api.Domain.Entities;

public class NotificationJob
{
    public Guid Id { get; set; }

    public long BookingId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime ScheduledSendTime { get; set; }

    public NotificationStatus Status { get; set; }

    public int RetryCount { get; set; }

    public NotificationChannel Channel { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
}
