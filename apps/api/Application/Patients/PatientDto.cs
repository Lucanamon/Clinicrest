namespace api.Application.Patients;

public class PatientDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? UnderlyingDisease { get; set; }

    public int Age { get; set; }

    public DateTime CreatedAt { get; set; }
}
