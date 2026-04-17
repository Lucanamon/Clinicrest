using api.Application.Abstractions;
using api.Application.Bookings;
using api.Application.Time;
using api.Domain;
using api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(
    IBookingService bookingService,
    ILogger<BookingsController> logger) : ControllerBase
{
    [Authorize(Roles = Roles.ClinicalAll)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingListItem>>> List(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var targetStatus = ParseStatus(status);
            var rows = await bookingService.GetListByStatusAsync(targetStatus, cancellationToken);
            return Ok(rows ?? Array.Empty<BookingListItem>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load bookings list.");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost]
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
            if (string.Equals(result.Error, "Slot Full or Invalid", StringComparison.Ordinal))
            {
                return Conflict(new { message = result.Error });
            }

            if (string.Equals(result.Error, "DuplicateBooking", StringComparison.Ordinal))
            {
                return Conflict(new { message = "A booking request for this name and phone number already exists." });
            }

            return BadRequest(new { message = result.Error });
        }

        var booking = result.Booking!;
        var response = new BookingDto
        {
            Id = booking.Id,
            SlotId = booking.SlotId,
            PatientName = booking.PatientName,
            PhoneNumber = booking.PhoneNumber,
            Status = booking.Status switch
            {
                BookingStatus.Active => "ACTIVE",
                BookingStatus.Scheduled => "SCHEDULED",
                _ => "CANCELLED"
            },
            CreatedAt = UtcInstant.AsUtcDateTimeOffset(booking.CreatedAt)
        };

        return Created($"/api/bookings/{booking.Id}", response);
    }

    [AllowAnonymous]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken = default)
    {
        var result = await bookingService.CancelAsync(id, cancellationToken);
        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.SlotMissing)
        {
            return BadRequest(new { message = "Could not update slot capacity for this cancellation." });
        }

        return Ok(new { success = true });
    }

    [Authorize(Roles = Roles.ClinicalAll)]
    [HttpPut("{id:long}/schedule")]
    public async Task<IActionResult> Schedule(
        long id,
        [FromBody] ScheduleBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PatientId == Guid.Empty)
        {
            return BadRequest(new { message = "patient_id is required." });
        }

        var updated = await bookingService.ScheduleAsync(id, request.PatientId, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return Ok(new { success = true });
    }

    private static BookingStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BookingStatus.Active;
        }

        if (string.Equals(status.Trim(), "SCHEDULED", StringComparison.OrdinalIgnoreCase))
        {
            return BookingStatus.Scheduled;
        }

        if (string.Equals(status.Trim(), "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            return BookingStatus.Cancelled;
        }

        return BookingStatus.Active;
    }
}
