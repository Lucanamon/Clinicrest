namespace api.Domain.Entities;

public class Booking
{
    public long Id { get; set; }

    public long SlotId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? DoctorId { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
