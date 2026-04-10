using System.ComponentModel.DataAnnotations;

namespace api.Application.Users;

public class UpdateProfileDto
{
    [MaxLength(120)]
    public string? DisplayName { get; set; }

    [MaxLength(2048)]
    public string? ProfileImageUrl { get; set; }
}
