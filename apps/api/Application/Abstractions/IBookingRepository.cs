using api.Application.Bookings;

namespace api.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingResult> CreateAsync(Guid userId, Guid slotId, CancellationToken cancellationToken = default);

    Task<CancelBookingResult> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
