using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Auth;

public class UserContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.Request.Headers.TryGetValue("X-Username", out var usernameValues))
        {
            var username = usernameValues.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

                if (user is not null)
                {
                    context.Items[UserHttpContextItem.Key] = user;
                }
            }
        }

        await next(context);
    }
}

public static class UserHttpContextItem
{
    public const string Key = "CurrentUser";
}
