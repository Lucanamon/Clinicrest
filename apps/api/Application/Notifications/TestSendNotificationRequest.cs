using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using api.Domain.Entities;

namespace api.Application.Notifications;

public class TestSendNotificationRequest : IValidatableObject
{
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    public string? EmailAddress { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<NotificationChannel>))]
    public NotificationChannel Channel { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            yield return new ValidationResult(
                "Message is required.",
                [nameof(Message)]);
        }

        if (Channel == NotificationChannel.Email)
        {
            if (string.IsNullOrWhiteSpace(EmailAddress))
            {
                yield return new ValidationResult(
                    "Email address is required when channel is Email.",
                    [nameof(EmailAddress)]);
            }
        }
        else if (Channel == NotificationChannel.Sms)
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                yield return new ValidationResult(
                    "Phone number is required when channel is Sms.",
                    [nameof(PhoneNumber)]);
            }
        }
    }
}
