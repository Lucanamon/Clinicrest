namespace api.Application.Patients;

public class PatientExportResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public string? GoogleSheetUrl { get; init; }
}
