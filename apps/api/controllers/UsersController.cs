using api.Application.Abstractions;
using api.Application.Users;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService, IUserRepository userRepository) : ControllerBase
{
    [HttpGet("doctors")]
    [Authorize(Roles = Roles.ClinicalAll)]
    public async Task<ActionResult<IReadOnlyList<DoctorListItemDto>>> GetDoctors(CancellationToken cancellationToken = default)
    {
        var doctors = await userRepository.GetDoctorsAsync(cancellationToken);
        var dto = doctors.Select(u => new DoctorListItemDto { Id = u.Id, Username = u.Username }).ToList();
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Roles = Roles.RootAdmin)]
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
