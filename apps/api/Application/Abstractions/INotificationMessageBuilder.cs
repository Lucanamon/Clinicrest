using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface INotificationMessageBuilder
{
    string BuildReminderMessage(
        NotificationChannel channel,
        string patientName,
        DateTime slotStartUtc);
}
