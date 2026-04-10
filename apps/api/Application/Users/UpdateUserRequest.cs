using System.ComponentModel.DataAnnotations;

namespace api.Application.Users;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ProfileImageUrl { get; set; }
}
