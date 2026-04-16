using System.Data;
using api.Application.Abstractions;
using api.Application.Bookings;
using api.Application.Time;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Infrastructure.Persistence.Repositories;

public class BookingRepository(ApplicationDbContext dbContext) : IBookingRepository
{
    private const int MaxSerializationRetries = 5;

    public async Task<BookingResult> CreateAsync(
        Guid? userId,
        string? phoneNumber,
        Guid slotId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateSingleAttemptAsync(userId, phoneNumber, slotId, cancellationToken);
            }
            catch (Exception ex) when (IsTransientPostgres(ex) && attempt < MaxSerializationRetries)
            {
                dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(25 * (1 << attempt)), cancellationToken);
            }
        }
    }

    private async Task<BookingResult> CreateSingleAttemptAsync(
        Guid? userId,
        string? phoneNumber,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var slot = await dbContext.TimeSlots
                .FromSqlInterpolated(
                    $"""
                    SELECT id, start_time, end_time, capacity, booked_count, created_at
                    FROM time_slots
                    WHERE id = {slotId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (slot is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed("Slot not found.");
            }

            var existingActive = userId.HasValue
                ? await dbContext.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        b => b.UserId == userId && b.SlotId == slotId && b.Status == "active",
                        cancellationToken)
                : await dbContext.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        b => b.PhoneNumber == phoneNumber && b.SlotId == slotId && b.Status == "active",
                        cancellationToken);

            if (existingActive is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return SuccessFromExisting(existingActive);
            }

            // Server clock only (never trust client-supplied times for eligibility).
            var nowUtc = DateTime.UtcNow;
            var slotStartUtc = UtcInstant.AsUtcDateTime(slot.StartTime);

            if (slotStartUtc < nowUtc)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed("Cannot book a slot that has already started.");
            }

            if (slot.BookedCount >= slot.Capacity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed("Slot is full.");
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PhoneNumber = phoneNumber,
                SlotId = slotId,
                Status = "active",
                CreatedAt = nowUtc
            };

            dbContext.Bookings.Add(booking);
            slot.BookedCount += 1;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BookingResult
            {
                IsSuccess = true,
                IsExistingBooking = false,
                Booking = booking
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            var existing = userId.HasValue
                ? await dbContext.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        b => b.UserId == userId && b.SlotId == slotId && b.Status == "active",
                        cancellationToken)
                : await dbContext.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        b => b.PhoneNumber == phoneNumber && b.SlotId == slotId && b.Status == "active",
                        cancellationToken);

            if (existing is not null)
            {
                return SuccessFromExisting(existing);
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static BookingResult SuccessFromExisting(Booking existing)
    {
        return new BookingResult
        {
            IsSuccess = true,
            IsExistingBooking = true,
            Booking = existing
        };
    }

    private static bool IsTransientPostgres(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg &&
                (pg.SqlState == PostgresErrorCodes.SerializationFailure ||
                 pg.SqlState == PostgresErrorCodes.DeadlockDetected))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<CancelBookingResult> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await dbContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CancelBookingResult.NotFoundResult();
            }

            if (string.Equals(booking.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.CommitAsync(cancellationToken);
                return CancelBookingResult.SuccessResult();
            }

            var slotId = booking.SlotId;
            var slot = await dbContext.TimeSlots
                .FromSqlInterpolated(
                    $"""
                    SELECT id, start_time, end_time, capacity, booked_count, created_at
                    FROM time_slots
                    WHERE id = {slotId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (slot is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CancelBookingResult.SlotMissingResult();
            }

            booking.Status = "cancelled";
            slot.BookedCount = Math.Max(0, slot.BookedCount - 1);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CancelBookingResult.SuccessResult();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static BookingResult Failed(string error)
    {
        return new BookingResult
        {
            IsSuccess = false,
            Error = error
        };
    }

    public async Task<IReadOnlyList<PhoneBookingDto>> GetByPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from b in dbContext.Bookings.AsNoTracking()
            join s in dbContext.TimeSlots.AsNoTracking() on b.SlotId equals s.Id
            where b.PhoneNumber == phoneNumber
            orderby s.StartTime descending
            select new PhoneBookingDto
            {
                Id = b.Id,
                SlotId = b.SlotId,
                StartTime = UtcInstant.AsUtcDateTimeOffset(s.StartTime),
                EndTime = UtcInstant.AsUtcDateTimeOffset(s.EndTime),
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        return rows;
    }
}
