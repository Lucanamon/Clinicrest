using api.Application.Abstractions;
using api.Application.Slots;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.RootAdmin + "," + Roles.Doctor + "," + Roles.Administrator)]
public class TimeSlotsController(ISlotService slotService) : ControllerBase
{
    [HttpPost("/api/time-slots")]
    public async Task<ActionResult<SlotDto>> Create(
        [FromBody] CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await slotService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Slot is null)
        {
            return BadRequest(new { message = result.Error ?? "Unable to create slot." });
        }

        return Created($"/api/time-slots/{result.Slot.Id}", result.Slot);
    }

    [HttpPatch("/api/time-slots/{id:long}/capacity")]
    public async Task<ActionResult<SlotDto>> UpdateCapacity(
        long id,
        [FromBody] UpdateSlotCapacityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await slotService.UpdateCapacityAsync(id, request.Action, cancellationToken);
        if (!result.IsSuccess || result.Slot is null)
        {
            if (string.Equals(result.Error, "Slot not found.", StringComparison.Ordinal))
            {
                return NotFound(new { message = result.Error });
            }

            return BadRequest(new { message = result.Error ?? "Unable to update slot capacity." });
        }

        return Ok(result.Slot);
    }

    [HttpDelete("/api/time-slots/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var result = await slotService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            if (string.Equals(result.Error, "Slot not found.", StringComparison.Ordinal))
            {
                return NotFound(new { message = result.Error });
            }

            return BadRequest(new { message = result.Error ?? "Unable to delete slot." });
        }

        return NoContent();
    }
}
