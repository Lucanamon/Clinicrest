using api.Application.Abstractions;
using api.Application.Bookings;

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

        return await bookingRepository.CreateAsync(slotId, patientName, cancellationToken);
    }

    public Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default) =>
        bookingRepository.CancelAsync(bookingId, cancellationToken);
}
