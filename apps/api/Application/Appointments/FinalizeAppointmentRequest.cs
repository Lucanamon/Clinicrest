using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api.Application.Appointments;

public class FinalizeAppointmentRequest
{
    [Required]
    [JsonPropertyName("booking_id")]
    public long BookingId { get; set; }

    [Required]
    [JsonPropertyName("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [JsonPropertyName("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [JsonPropertyName("appointment_date")]
    public DateTime AppointmentDate { get; set; }

    [JsonPropertyName("phone_number")]
    [RegularExpression(@"^[0-9+]*$")]
    public string? PhoneNumber { get; set; }

    [MaxLength(2000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
