namespace api.Application.Users;

public class DoctorListItemDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;
}

public class UserListItemDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
