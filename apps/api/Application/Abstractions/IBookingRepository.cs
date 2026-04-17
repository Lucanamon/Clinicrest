using api.Application.Bookings;

namespace api.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingResult> CreateAsync(
        long slotId,
        string patientName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default);

    Task<bool> ScheduleAsync(long bookingId, Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingListItem>> GetActiveListAsync(CancellationToken cancellationToken = default);
}
