using System.ComponentModel.DataAnnotations;

namespace api.Application.Appointments;

public class UpdateAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>Required when acting user is Admin; ignored for Doctor.</summary>
    public Guid? DoctorId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
