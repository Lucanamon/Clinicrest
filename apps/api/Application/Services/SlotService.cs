using api.Application.Abstractions;
using api.Application.Slots;
using api.Application.Time;
using api.Domain.Entities;
using api.Infrastructure.Persistence;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Application.Services;

public class SlotService(ISlotRepository slotRepository, ApplicationDbContext dbContext) : ISlotService
{
    public async Task<IReadOnlyList<SlotDto>> GetAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var slots = await slotRepository.GetAllAsync(date, cancellationToken);
        return slots.Select(MapToDto).ToList();
    }

    public async Task<(bool IsSuccess, string? Error, SlotDto? Slot)> CreateAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var startUtc = request.StartTime.UtcDateTime;
        var endUtc = request.EndTime.UtcDateTime;
        if (endUtc <= startUtc)
        {
            return (false, "end_time must be greater than start_time.", null);
        }

        if (request.Capacity <= 0)
        {
            return (false, "capacity must be greater than 0.", null);
        }

        var created = await slotRepository.CreateAsync(startUtc, endUtc, request.Capacity, cancellationToken);
        return (true, null, MapToDto(created));
    }

    public async Task<(bool IsSuccess, string? Error, SlotDto? Slot)> UpdateCapacityAsync(
        long slotId,
        string action,
        CancellationToken cancellationToken = default)
    {
        var normalizedAction = action.Trim().ToLowerInvariant();
        if (normalizedAction is not ("increase" or "decrease"))
        {
            return (false, "action must be either 'increase' or 'decrease'.", null);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var slot = await slotRepository.GetForUpdateAsync(slotId, cancellationToken);
            if (slot is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Slot not found.", null);
            }

            if (normalizedAction == "increase")
            {
                slot.Capacity += 1;
            }
            else
            {
                var nextCapacity = slot.Capacity - 1;
                if (nextCapacity < 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "capacity cannot be negative.", null);
                }

                if (nextCapacity < slot.BookedCount)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "capacity cannot be less than booked_count.", null);
                }

                slot.Capacity = nextCapacity;
            }

            await slotRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null, MapToDto(slot));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool IsSuccess, string? Error)> DeleteAsync(long slotId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var slot = await slotRepository.GetForUpdateAsync(slotId, cancellationToken);
            if (slot is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Slot not found.");
            }

            if (slot.BookedCount > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Cannot delete a slot that still has active bookings.");
            }

            await dbContext.Bookings
                .Where(b => b.SlotId == slotId)
                .ExecuteDeleteAsync(cancellationToken);

            dbContext.Slots.Remove(slot);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static SlotDto MapToDto(Slot slot)
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
