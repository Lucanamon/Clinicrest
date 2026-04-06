namespace api.Application.Backlogs;

public class BacklogDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Priority { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public Guid AssignedToUserId { get; set; }

    public string AssignedToName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
