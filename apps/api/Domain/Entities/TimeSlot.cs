namespace api.Domain.Entities;

public class TimeSlot
{
    public Guid Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Capacity { get; set; }

    public int BookedCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
