using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class RescheduleBookingRequest
{
    [JsonPropertyName("new_slot_id")]
    public long? NewSlotId { get; set; }

    [JsonPropertyName("newSlotId")]
    public long? NewSlotIdCamel { get; set; }

    public long ResolveNewSlotId() => NewSlotId ?? NewSlotIdCamel ?? 0;
}
