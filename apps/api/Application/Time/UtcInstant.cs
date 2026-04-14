namespace api.Application.Time;

/// <summary>
/// Normalizes instants read from PostgreSQL (timestamptz) for API output and comparisons.
/// All booking/slot business rules use <see cref="DateTime.UtcNow"/> only — never client clocks.
/// </summary>
public static class UtcInstant
{
    public static DateTime AsUtcDateTime(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>Serializes as ISO-8601 with explicit UTC offset in JSON.</summary>
    public static DateTimeOffset AsUtcDateTimeOffset(DateTime value) =>
        new(AsUtcDateTime(value), TimeSpan.Zero);
}
