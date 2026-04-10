namespace api.Domain.Entities;

public class Appointment : AuditableEntity
{
    public Guid PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }

    public User Doctor { get; set; } = null!;

    public DateTime AppointmentDate { get; set; }

    public required string Status { get; set; }

    public string? Notes { get; set; }
}
