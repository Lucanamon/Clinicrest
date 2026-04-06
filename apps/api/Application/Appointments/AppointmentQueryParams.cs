namespace api.Application.Appointments;

public class AppointmentQueryParams
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    /// <summary>Matches patient first or last name (ILIKE).</summary>
    public string? SearchTerm { get; set; }

    public string? Status { get; set; }

    public DateTime? FromAppointmentDate { get; set; }

    public DateTime? ToAppointmentDate { get; set; }

    public string? SortBy { get; set; }

    /// <summary>asc or desc (case-insensitive).</summary>
    public string? SortDirection { get; set; }
}
