using api.Domain.Entities;

namespace api.Application.Bookings;

public class BookingResult
{
    public bool IsSuccess { get; set; }

    public string? Error { get; set; }

    public Booking? Booking { get; set; }
}
