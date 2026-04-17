using api.Application.Bookings;

namespace api.Application.Abstractions;

public interface IBookingService
{
    Task<BookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingListItem>> GetActiveListAsync(CancellationToken cancellationToken = default);
}
