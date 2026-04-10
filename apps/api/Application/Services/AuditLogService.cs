using api.Application.Abstractions;
using api.Application.AuditLogs;
using api.Application.Patients;
using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.Application.Services;

public class AuditLogService(ApplicationDbContext dbContext) : IAuditLogService
{
    public async Task<PagedResult<AuditLogListItemDto>> GetPagedAsync(
        AuditLogQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 200);

        var baseQuery = dbContext.AuditLogs.AsNoTracking().OrderByDescending(a => a.Timestamp);
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var actorIds = rows
            .Select(r => Guid.TryParse(r.UserId, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        var usernames = await dbContext.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var items = rows.Select(r =>
        {
            string? actorUsername = null;
            if (Guid.TryParse(r.UserId, out var uid))
            {
                usernames.TryGetValue(uid, out actorUsername);
            }

            return new AuditLogListItemDto
            {
                Id = r.Id,
                UserId = r.UserId,
                ActorUsername = actorUsername,
                Action = r.Action,
                EntityName = r.EntityName,
                EntityId = r.EntityId,
                OldValues = r.OldValues,
                NewValues = r.NewValues,
                Timestamp = r.Timestamp
            };
        }).ToList();

        return new PagedResult<AuditLogListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
