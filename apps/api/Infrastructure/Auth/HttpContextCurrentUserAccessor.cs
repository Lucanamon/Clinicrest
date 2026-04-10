using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace api.Infrastructure.Auth;

public sealed class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string GetAuditUserId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return "(system)";
        }

        var principal = httpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            return "(system)";
        }

        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return string.IsNullOrWhiteSpace(id) ? "(system)" : id;
    }
}
