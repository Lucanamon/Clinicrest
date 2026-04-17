using System.Text.Json.Serialization;

namespace api.Application.Bookings;

public class ScheduleBookingRequest
{
    [JsonPropertyName("patient_id")]
    public Guid PatientId { get; set; }
}
