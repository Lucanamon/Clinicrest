namespace api.Application.Bookings;

public class CancelBookingResult
{
    public bool NotFound { get; init; }

    public bool SlotMissing { get; init; }

    public bool Success { get; init; }

    public static CancelBookingResult NotFoundResult() => new() { NotFound = true };

    public static CancelBookingResult SlotMissingResult() => new() { SlotMissing = true };

    public static CancelBookingResult SuccessResult() => new() { Success = true };
}
