using System.ComponentModel.DataAnnotations;

namespace api.Application.Users;

public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Doctor, Nurse, or Administrator (never RootAdmin).</summary>
    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ProfileImageUrl { get; set; }
}
