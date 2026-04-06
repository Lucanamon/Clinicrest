using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Application.Abstractions;
using api.Application.Backlogs;
using api.Application.Patients;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
public class BacklogsController(IBacklogService backlogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BacklogDto>>> GetBacklogs(
        [FromQuery] BacklogQueryParams query,
        CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
        {
            query.PageNumber = 1;
        }

        if (query.PageSize < 1)
        {
            query.PageSize = 10;
        }

        if (query.PageSize > 100)
        {
            query.PageSize = 100;
        }

        var userId = TryGetUserId(User);
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        var result = await backlogService.GetPagedAsync(query, userId.Value, role, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BacklogDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId(User);
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        var backlog = await backlogService.GetByIdAsync(id, userId.Value, role, cancellationToken);
        if (backlog is null)
        {
            return NotFound();
        }

        return Ok(backlog);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<BacklogDto>> Create(
        [FromBody] CreateBacklogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = await backlogService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBacklogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = TryGetUserId(User);
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await backlogService.UpdateAsync(id, request, userId.Value, role, cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await backlogService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
