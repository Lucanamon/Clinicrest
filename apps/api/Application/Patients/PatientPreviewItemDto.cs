namespace api.Application.Patients;

public class PatientPreviewItemDto
{
    public required string Name { get; init; }

    public int Age { get; init; }

    public required string Phone { get; init; }

    public string? Disease { get; init; }

    public DateTime CreatedAt { get; init; }
}
