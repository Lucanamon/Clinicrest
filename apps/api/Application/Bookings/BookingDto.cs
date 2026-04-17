using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class BookingDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("slot_id")]
    public long SlotId { get; set; }

    [JsonPropertyName("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ACTIVE";

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
