namespace api.Domain.Entities;

public class Backlog
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required string Priority { get; set; }

    public required string Status { get; set; }

    public Guid AssignedToUserId { get; set; }

    public User AssignedTo { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
