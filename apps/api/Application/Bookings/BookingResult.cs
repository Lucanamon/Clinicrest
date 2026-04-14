using api.Domain.Entities;

namespace api.Application.Bookings;

public class BookingResult
{
    public bool IsSuccess { get; set; }

    /// <summary>
    /// True when the same user already had an active booking for this slot (idempotent replay).
    /// </summary>
    public bool IsExistingBooking { get; set; }

    public string? Error { get; set; }

    public Booking? Booking { get; set; }
}
