using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class CreateBookingRequest
{
    [JsonPropertyName("user_id")]
    public Guid? UserId { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("slot_id")]
    public Guid? SlotId { get; set; }

    [JsonPropertyName("slotId")]
    public Guid? SlotIdCamel { get; set; }

    public Guid ResolveSlotId() => SlotId ?? SlotIdCamel ?? Guid.Empty;
}
