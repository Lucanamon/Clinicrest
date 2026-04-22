using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using api.Domain.Entities;

namespace api.Application.Notifications;

public class TestSendNotificationRequest
{
    [Required]
    [MinLength(1)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<NotificationChannel>))]
    public NotificationChannel Channel { get; set; }
}
