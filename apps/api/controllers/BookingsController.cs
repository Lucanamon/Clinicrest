using api.Application.Abstractions;
using api.Application.Bookings;
using api.Application.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/api/bookings")]
    public async Task<ActionResult<IReadOnlyList<PhoneBookingDto>>> GetByPhone(
        [FromQuery] string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await bookingService.GetByPhoneAsync(phoneNumber, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Items);
    }

    [AllowAnonymous]
    [HttpPost("/api/bookings")]
    public async Task<ActionResult<BookingDto>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await bookingService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            if (string.Equals(result.Error, "Slot is full.", StringComparison.Ordinal))
            {
                return Conflict(new { message = result.Error });
            }

            if (string.Equals(
                    result.Error,
                    "Cannot book a slot that has already started.",
                    StringComparison.Ordinal))
            {
                return Conflict(new { message = result.Error });
            }

            return BadRequest(new { message = result.Error });
        }

        var booking = result.Booking!;
        var response = new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            PhoneNumber = booking.PhoneNumber,
            SlotId = booking.SlotId,
            Status = booking.Status,
            CreatedAt = UtcInstant.AsUtcDateTimeOffset(booking.CreatedAt)
        };

        if (result.IsExistingBooking)
        {
            return Ok(response);
        }

        return Created($"/api/bookings/{booking.Id}", response);
    }

    [AllowAnonymous]
    [HttpDelete("/api/bookings/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await bookingService.CancelAsync(id, cancellationToken);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.SlotMissing)
        {
            return BadRequest(new { message = "Time slot for this booking no longer exists." });
        }

        return Ok(new { success = true });
    }
}
