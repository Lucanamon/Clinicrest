using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class CreateBookingRequest
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("slot_id")]
    public Guid SlotId { get; set; }
}
