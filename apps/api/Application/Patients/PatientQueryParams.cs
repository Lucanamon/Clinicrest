namespace api.Application.Patients;

public class PatientQueryParams
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }

    public string? Gender { get; set; }

    public DateTime? FromDateOfBirth { get; set; }

    public DateTime? ToDateOfBirth { get; set; }

    public string? SortBy { get; set; }

    /// <summary>asc or desc (case-insensitive).</summary>
    public string? SortDirection { get; set; }
}
