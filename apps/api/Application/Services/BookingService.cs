using api.Application.Abstractions;
using api.Application.Bookings;
using System.Text.RegularExpressions;

namespace api.Application.Services;

public class BookingService(IBookingRepository bookingRepository) : IBookingService
{
    private static readonly Regex PhoneDigitsRegex = new("^[0-9]+$", RegexOptions.Compiled);

    public async Task<BookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var slotId = request.ResolveSlotId();
        if (slotId == Guid.Empty)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "slot_id or slotId is required."
            };
        }

        var hasUser = request.UserId is { } uid && uid != Guid.Empty;
        var hasPhoneText = !string.IsNullOrWhiteSpace(request.PhoneNumber);

        if (hasUser && hasPhoneText)
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = "Provide either user_id or phoneNumber, not both."
            };
        }

        if (hasUser)
        {
            return await bookingRepository.CreateAsync(request.UserId, null, slotId, cancellationToken);
        }

        if (!TryNormalizePhone(request.PhoneNumber, out var phone, out var phoneError))
        {
            return new BookingResult
            {
                IsSuccess = false,
                Error = phoneError!
            };
        }

        return await bookingRepository.CreateAsync(null, phone, slotId, cancellationToken);
    }

    private static bool TryNormalizePhone(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "phoneNumber is required when user_id is omitted.";
            return false;
        }

        var trimmed = raw.Trim();
        if (!PhoneDigitsRegex.IsMatch(trimmed))
        {
            error = "phoneNumber must contain digits only.";
            return false;
        }

        if (trimmed.Length is < 9 or > 10)
        {
            error = "phoneNumber length must be 9 to 10 digits.";
            return false;
        }

        normalized = trimmed;
        return true;
    }

    public Task<CancelBookingResult> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        bookingRepository.CancelAsync(bookingId, cancellationToken);

    public async Task<(bool IsSuccess, string? Error, IReadOnlyList<PhoneBookingDto> Items)> GetByPhoneAsync(
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePhone(phoneNumber, out var normalized, out var error))
        {
            return (false, error, Array.Empty<PhoneBookingDto>());
        }

        var items = await bookingRepository.GetByPhoneAsync(normalized!, cancellationToken);
        return (true, null, items);
    }
}
