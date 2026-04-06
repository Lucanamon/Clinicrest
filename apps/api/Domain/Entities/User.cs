namespace api.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public required string Role { get; set; }

    public DateTime CreatedAt { get; set; }
}

