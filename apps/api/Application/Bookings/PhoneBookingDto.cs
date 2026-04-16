using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class PhoneBookingDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("slot_id")]
    public Guid SlotId { get; set; }

    [JsonPropertyName("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";
}
