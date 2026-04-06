using System.ComponentModel.DataAnnotations;

namespace api.Application.Backlogs;

public class UpdateBacklogRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(16)]
    public string Priority { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Status { get; set; } = string.Empty;

    [Required]
    public Guid AssignedToUserId { get; set; }
}
