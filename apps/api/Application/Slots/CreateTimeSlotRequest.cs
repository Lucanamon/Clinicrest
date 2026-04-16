using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api.Application.Slots;

public class CreateTimeSlotRequest
{
    [Required]
    [JsonPropertyName("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [Required]
    [JsonPropertyName("end_time")]
    public DateTimeOffset EndTime { get; set; }

    [Range(1, int.MaxValue)]
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }
}
