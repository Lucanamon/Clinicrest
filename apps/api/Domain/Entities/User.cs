namespace api.Domain.Entities;

public class User : AuditableEntity
{
    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public required string Role { get; set; }
}
