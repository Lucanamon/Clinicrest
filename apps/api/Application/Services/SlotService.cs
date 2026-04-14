using api.Application.Abstractions;
using api.Application.Slots;
using api.Application.Time;
using api.Domain.Entities;

namespace api.Application.Services;

public class SlotService(ISlotRepository slotRepository) : ISlotService
{
    public async Task<IReadOnlyList<SlotDto>> GetAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var slots = await slotRepository.GetAllAsync(date, cancellationToken);
        return slots.Select(MapToDto).ToList();
    }

    private static SlotDto MapToDto(TimeSlot slot)
    {
        var capacity = Math.Max(slot.Capacity, 0);
        var bookedCount = Math.Clamp(slot.BookedCount, 0, capacity);

        return new SlotDto
        {
            Id = slot.Id,
            StartTime = UtcInstant.AsUtcDateTimeOffset(slot.StartTime),
            EndTime = UtcInstant.AsUtcDateTimeOffset(slot.EndTime),
            Capacity = capacity,
            BookedCount = bookedCount,
            AvailableSlots = Math.Max(0, capacity - bookedCount)
        };
    }
}
