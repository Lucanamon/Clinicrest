using api.Application.Abstractions;
using api.Application.Bookings;
using api.Domain.Entities;

namespace api.Application.Services;

public class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    public async Task<BookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var slotId = request.ResolveSlotId();
        if (slotId <= 0)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "slot_id is required."
            };
        }

        var patientName = request.ResolvePatientName();
        if (string.IsNullOrWhiteSpace(patientName))
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "patient_name is required."
            };
        }

        var phoneNumber = request.ResolvePhoneNumber();
        return await bookingRepository.CreateAsync(slotId, patientName, phoneNumber, cancellationToken);
    }

    public Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default) =>
        bookingRepository.CancelAsync(bookingId, cancellationToken);

    public Task<bool> ScheduleAsync(long bookingId, Guid patientId, CancellationToken cancellationToken = default) =>
        bookingRepository.ScheduleAsync(bookingId, patientId, cancellationToken);

    public Task<RescheduleBookingResult> RescheduleAsync(
        long bookingId,
        long newSlotId,
        CancellationToken cancellationToken = default) =>
        bookingRepository.RescheduleAsync(bookingId, newSlotId, cancellationToken);

    public Task<IReadOnlyList<BookingListItem>> GetListByStatusAsync(
        BookingStatus status,
        CancellationToken cancellationToken = default) =>
        bookingRepository.GetListByStatusAsync(status, cancellationToken);

    public Task<bool> ResetFailedNotificationForRetryAsync(
        long bookingId,
        CancellationToken cancellationToken = default) =>
        bookingRepository.ResetFailedNotificationForRetryAsync(bookingId, cancellationToken);
}
