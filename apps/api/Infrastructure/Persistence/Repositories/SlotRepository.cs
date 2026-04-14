using api.Application.Abstractions;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class SlotRepository(ApplicationDbContext dbContext) : ISlotRepository
{
    public async Task<IReadOnlyList<TimeSlot>> GetAllAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        IQueryable<TimeSlot> query = dbContext.TimeSlots
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
}
