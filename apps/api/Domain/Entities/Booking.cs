namespace api.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SlotId { get; set; }

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }
}
