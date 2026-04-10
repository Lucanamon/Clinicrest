using api.Application.Abstractions;
using api.Application.Backlogs;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class BacklogRepository(ApplicationDbContext dbContext) : IBacklogRepository
{
    public async Task AddAsync(Backlog backlog, CancellationToken cancellationToken = default)
    {
        dbContext.Backlogs.Add(backlog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var backlog = await dbContext.Backlogs.FirstOrDefaultAsync(
            b => b.Id == id && !b.IsDeleted,
            cancellationToken);
        if (backlog is null)
        {
            return false;
        }

        dbContext.Backlogs.Remove(backlog);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Backlog> Items, int TotalCount)> GetPagedAsync(
        BacklogQueryParams queryParams,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Backlog> query = dbContext.Backlogs
            .AsNoTracking()
            .Include(b => b.AssignedTo)
            .Where(b => !b.IsDeleted && !b.AssignedTo.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(b => b.AssignedToUserId == restrictToDoctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = SanitizeLikeTerm(queryParams.SearchTerm);
            if (term.Length > 0)
            {
                var pattern = $"%{term}%";
                query = query.Where(b => EF.Functions.ILike(b.Title, pattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            var status = queryParams.Status.Trim();
            query = query.Where(b => EF.Functions.ILike(b.Status, status));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Priority))
        {
            var priority = queryParams.Priority.Trim();
            query = query.Where(b => EF.Functions.ILike(b.Priority, priority));
        }

        query = ApplyOrdering(query, queryParams);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = queryParams.PageNumber;
        var pageSize = queryParams.PageSize;
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Backlog?> GetByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default)
    {
        IQueryable<Backlog> query = dbContext.Backlogs
            .AsNoTracking()
            .Include(b => b.AssignedTo)
            .Where(b => b.Id == id && !b.IsDeleted && !b.AssignedTo.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(b => b.AssignedToUserId == restrictToDoctorId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Backlog?> GetTrackedByIdAsync(
        Guid id,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Backlog> query = dbContext.Backlogs
            .Where(b => b.Id == id && !b.IsDeleted && !b.AssignedTo.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(b => b.AssignedToUserId == restrictToDoctorId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Backlog backlog, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static readonly HashSet<string> AllowedSortProperties =
    [
        nameof(Backlog.Title),
        nameof(Backlog.Priority),
        nameof(Backlog.Status),
        nameof(Backlog.CreatedAt)
    ];

    private static IQueryable<Backlog> ApplyOrdering(IQueryable<Backlog> queryable, BacklogQueryParams queryParams)
    {
        if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
        {
            var sortBy = queryParams.SortBy.Trim();
            if (AllowedSortProperties.Contains(sortBy))
            {
                if (string.Equals(queryParams.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    return queryable.OrderByDescending(x => EF.Property<object>(x, sortBy));
                }

                return queryable.OrderBy(x => EF.Property<object>(x, sortBy));
            }
        }

        return queryable.OrderByDescending(x => x.CreatedAt);
    }

    private static string SanitizeLikeTerm(string value)
    {
        return value.Trim().Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
