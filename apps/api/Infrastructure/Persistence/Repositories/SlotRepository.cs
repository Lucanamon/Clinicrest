using api.Application.Abstractions;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class SlotRepository(ApplicationDbContext dbContext) : ISlotRepository
{
    public async Task<IReadOnlyList<Slot>> GetAllAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        IQueryable<Slot> query = dbContext.Slots
            .AsNoTracking();

        if (date.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var endUtc = startUtc.AddDays(1);
            query = query.Where(s => s.StartTime >= startUtc && s.StartTime < endUtc);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Slot> CreateAsync(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        var slot = new Slot
        {
            StartTime = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(endTimeUtc, DateTimeKind.Utc),
            Capacity = capacity,
            BookedCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Slots.Add(slot);
        await dbContext.SaveChangesAsync(cancellationToken);
        return slot;
    }

    public Task<Slot?> GetForUpdateAsync(long slotId, CancellationToken cancellationToken = default)
    {
        return dbContext.Slots
            .FromSqlInterpolated(
                $"""
                SELECT id, start_time, end_time, capacity, booked_count, created_at
                FROM slots
                WHERE id = {slotId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
