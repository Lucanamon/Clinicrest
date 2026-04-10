using api.Application.Abstractions;
using api.Application.Users;
using api.Domain;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    IUserService userService,
    IUserRepository userRepository,
    ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken = default)
    {
        var username = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new { message = "Authenticated username is missing." });
        }

        var user = await userService.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMyProfile(
        [FromBody] UpdateProfileDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var headerUsername = HttpContext.Request.Headers["X-Username"].ToString();
        var username = !string.IsNullOrWhiteSpace(headerUsername)
            ? headerUsername
            : (User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name));
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Missing username. Provide JWT identity or X-Username header." });
        }

        try
        {
            var updated = await userService.UpdateProfileAsync(username, request, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update profile for username {Username}", username);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unexpected server error while updating profile.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    [HttpGet("doctors")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<IReadOnlyList<DoctorListItemDto>>> GetDoctors(CancellationToken cancellationToken = default)
    {
        var doctors = await userRepository.GetDoctorsAsync(cancellationToken);
        var dto = doctors.Select(u => new DoctorListItemDto { Id = u.Id, Username = u.Username }).ToList();
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    [Authorize(Roles = Roles.RootAdmin)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = await userService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.RootAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await userService.DeleteAsync(id, cancellationToken);
            if (!deleted)
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
}
