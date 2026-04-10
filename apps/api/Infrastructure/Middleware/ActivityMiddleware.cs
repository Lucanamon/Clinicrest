using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Middleware;

public class ActivityMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context, ApplicationDbContext db)
    {
        var username = context.Request.Headers["X-Username"].ToString();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = await db.Users.FirstOrDefaultAsync(
                u => u.Username == username,
                context.RequestAborted);

            if (user is not null)
            {
                user.LastActiveAt = DateTime.UtcNow;
                await db.SaveChangesAsync(context.RequestAborted);
            }
        }

        await _next(context);
    }
}
