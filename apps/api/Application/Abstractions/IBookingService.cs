using api.Application.Bookings;
using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface IBookingService
{
    Task<BookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default);

    Task<bool> ScheduleAsync(long bookingId, Guid patientId, CancellationToken cancellationToken = default);

    Task<RescheduleBookingResult> RescheduleAsync(
        long bookingId,
        long newSlotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingListItem>> GetListByStatusAsync(
        BookingStatus status,
        CancellationToken cancellationToken = default);

    Task<bool> ResetFailedNotificationForRetryAsync(
        long bookingId,
        CancellationToken cancellationToken = default);
}
