using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface ISlotRepository
{
    Task<IReadOnlyList<TimeSlot>> GetAllAsync(DateOnly? date, CancellationToken cancellationToken = default);
}
