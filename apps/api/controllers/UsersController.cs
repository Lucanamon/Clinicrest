using api.Application.Abstractions;
using api.Application.Users;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("doctors")]
    public async Task<ActionResult<IReadOnlyList<DoctorListItemDto>>> GetDoctors(CancellationToken cancellationToken = default)
    {
        var doctors = await userRepository.GetDoctorsAsync(cancellationToken);
        var dto = doctors.Select(u => new DoctorListItemDto { Id = u.Id, Username = u.Username }).ToList();
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var dto = users.Select(u => new UserListItemDto { Id = u.Id, Username = u.Username, Role = u.Role }).ToList();
        return Ok(dto);
    }
}
