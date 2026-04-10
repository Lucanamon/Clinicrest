namespace api.Application.Users;

public class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public DateTime LastActiveAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
