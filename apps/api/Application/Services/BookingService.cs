using api.Application.Abstractions;
using api.Application.Bookings;

namespace api.Application.Services;

public class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    public async Task<BookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "user_id is required."
            };
        }

        if (request.SlotId == Guid.Empty)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "slot_id is required."
            };
        }

        return await bookingRepository.CreateAsync(request.UserId, request.SlotId, cancellationToken);
    }

    public Task<CancelBookingResult> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        bookingRepository.CancelAsync(bookingId, cancellationToken);
}
