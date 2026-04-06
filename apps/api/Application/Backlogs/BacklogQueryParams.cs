namespace api.Application.Backlogs;

public class BacklogQueryParams
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public string? SortBy { get; set; }

    /// <summary>asc or desc (case-insensitive).</summary>
    public string? SortDirection { get; set; }
}
