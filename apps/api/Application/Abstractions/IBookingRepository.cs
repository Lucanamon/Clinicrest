using api.Application.Bookings;

namespace api.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingResult> CreateAsync(
        long slotId,
        string patientName,
        CancellationToken cancellationToken = default);

    Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default);
}
