namespace api.Domain.Entities;

public class User : AuditableEntity
{
    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public required string Role { get; set; }

    public string? DisplayName { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}
