using System.ComponentModel.DataAnnotations;

namespace api.Application.Patients;

public class CreatePatientRequest
{
    [Required]
    [MaxLength(200)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(32)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(500)]
    public string? Email { get; set; }

    public bool AllowSms { get; set; } = true;

    public bool AllowEmail { get; set; } = true;

    [MaxLength(1000)]
    public string? UnderlyingDisease { get; set; }
}
