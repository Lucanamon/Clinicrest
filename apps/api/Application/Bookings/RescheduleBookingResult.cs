namespace api.Application.Bookings;

public class RescheduleBookingResult
{
    public bool IsSuccess { get; init; }

    public bool NotFound { get; init; }

    public string? Error { get; init; }

    public static RescheduleBookingResult SuccessResult() => new() { IsSuccess = true };

    public static RescheduleBookingResult NotFoundResult() => new() { NotFound = true };

    public static RescheduleBookingResult Fail(string error) => new() { Error = error };
}
