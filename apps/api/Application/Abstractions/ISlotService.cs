using api.Application.Slots;

namespace api.Application.Abstractions;

public interface ISlotService
{
    Task<IReadOnlyList<SlotDto>> GetAsync(DateOnly? date, CancellationToken cancellationToken = default);

    Task<(bool IsSuccess, string? Error, SlotDto? Slot)> CreateAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool IsSuccess, string? Error, SlotDto? Slot)> UpdateCapacityAsync(
        Guid slotId,
        string action,
        CancellationToken cancellationToken = default);

    Task<(bool IsSuccess, string? Error)> DeleteAsync(Guid slotId, CancellationToken cancellationToken = default);
}
