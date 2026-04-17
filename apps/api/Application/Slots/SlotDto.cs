using System.Text.Json.Serialization;

namespace api.Application.Slots;

public class SlotDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("booked_count")]
    public int BookedCount { get; set; }

    [JsonPropertyName("available_slots")]
    public int AvailableSlots { get; set; }
}
