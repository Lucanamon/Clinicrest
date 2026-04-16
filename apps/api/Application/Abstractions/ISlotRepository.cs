using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface ISlotRepository
{
    Task<IReadOnlyList<TimeSlot>> GetAllAsync(DateOnly? date, CancellationToken cancellationToken = default);

    Task<TimeSlot> CreateAsync(DateTime startTimeUtc, DateTime endTimeUtc, int capacity, CancellationToken cancellationToken = default);

    Task<TimeSlot?> GetForUpdateAsync(Guid slotId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
