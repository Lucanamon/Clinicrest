using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Application.Abstractions;
using api.Application.Search;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = Roles.ClinicalAll)]
public class SearchController(IGlobalSearchService globalSearchService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<GlobalSearchResult>> Search(
        [FromQuery] string? query,
        CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId(User);
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var result = await globalSearchService.SearchAsync(query ?? string.Empty, userId.Value, role, cancellationToken);
        return Ok(result);
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
