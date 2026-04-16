using api.Application.Abstractions;
using api.Application.Slots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SlotsController(ISlotService slotService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/api/slots")]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> GetSlots(
        [FromQuery] SlotQueryParams query,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDate(query.Date, out var date))
        {
            return BadRequest(new { message = "date must be in YYYY-MM-DD format." });
        }

        var result = await slotService.GetAsync(date, cancellationToken);
        return Ok(result);
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return false;
        }

        date = parsed;
        return true;
    }
}
