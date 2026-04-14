using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class BookingDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("slot_id")]
    public Guid SlotId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
