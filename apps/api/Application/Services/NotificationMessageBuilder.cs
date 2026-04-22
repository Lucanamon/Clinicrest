using api.Application.Abstractions;
using api.Domain.Entities;

namespace api.Application.Services;

public class NotificationMessageBuilder : INotificationMessageBuilder
{
    public string BuildReminderMessage(
        NotificationChannel channel,
        string patientName,
        DateTime slotStartUtc)
    {
        var safeName = string.IsNullOrWhiteSpace(patientName) ? "Patient" : patientName.Trim();
        var localTime = slotStartUtc.ToLocalTime();
        var dateText = localTime.ToString("ddd, MMM d yyyy");
        var timeText = localTime.ToString("hh:mm tt");

        return channel == NotificationChannel.Sms
            ? $"Clinicrest reminder for {safeName}: your appointment is on {dateText} at {timeText}. Please arrive 10 minutes early."
            : $"Hello {safeName}, this is a reminder from Clinicrest. Your appointment is scheduled on {dateText} at {timeText}. Please arrive 10 minutes early. If you need to reschedule, contact the clinic.";
    }
}
