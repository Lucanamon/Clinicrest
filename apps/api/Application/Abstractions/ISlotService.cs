using api.Application.Slots;

namespace api.Application.Abstractions;

public interface ISlotService
{
    Task<IReadOnlyList<SlotDto>> GetAsync(DateOnly? date, CancellationToken cancellationToken = default);
}
