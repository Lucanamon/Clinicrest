using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface ISlotRepository
{
    Task<IReadOnlyList<Slot>> GetAllAsync(DateOnly? date, CancellationToken cancellationToken = default);

    Task<Slot> CreateAsync(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        int capacity,
        CancellationToken cancellationToken = default);

    Task<Slot?> GetForUpdateAsync(long slotId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
