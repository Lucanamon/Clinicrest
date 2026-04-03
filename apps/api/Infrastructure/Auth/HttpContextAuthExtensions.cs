using api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace api.Infrastructure.Auth;

public static class HttpContextAuthExtensions
{
    public static User? GetCurrentUser(this HttpContext context)
    {
        return context.Items.TryGetValue(UserHttpContextItem.Key, out var userObj)
            ? userObj as User
            : null;
    }

    public static ActionResult? RequireAuthenticatedUser(this ControllerBase controller)
    {
        return controller.HttpContext.GetCurrentUser() is null
            ? controller.Unauthorized(new { message = "Missing or invalid X-Username header." })
            : null;
    }
}
