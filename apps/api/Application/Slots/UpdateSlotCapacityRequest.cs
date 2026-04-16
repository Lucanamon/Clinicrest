using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api.Application.Slots;

public class UpdateSlotCapacityRequest
{
    [Required]
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
}
