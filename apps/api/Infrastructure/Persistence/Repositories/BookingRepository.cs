using System.Data;
using api.Application.Abstractions;
using api.Application.Bookings;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Infrastructure.Persistence.Repositories;

public class BookingRepository(ApplicationDbContext dbContext) : IBookingRepository
{
    private const int MaxSerializationRetries = 5;

    public async Task<BookingResult> CreateAsync(
        long slotId,
        string patientName,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateSingleAttemptAsync(slotId, patientName, cancellationToken);
            }
            catch (Exception ex) when (IsTransientPostgres(ex) && attempt < MaxSerializationRetries)
            {
                dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(25 * (1 << attempt)), cancellationToken);
            }
        }
    }

    private async Task<BookingResult> CreateSingleAttemptAsync(
        long slotId,
        string patientName,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var rows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE slots
                SET booked_count = booked_count + 1
                WHERE id = {slotId}
                  AND booked_count < capacity
                  AND start_time > NOW()
                """,
                cancellationToken);

            if (rows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failed("Slot Full or Invalid");
            }

            var booking = new Booking
            {
                SlotId = slotId,
                PatientName = patientName,
                Status = BookingStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BookingResult
            {
                IsSuccess = true,
                Booking = booking
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CancelBookingResult> CancelAsync(long bookingId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CancelBookingResult.NotFoundResult();
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken);
                return CancelBookingResult.SuccessResult();
            }

            var decremented = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE slots
                SET booked_count = booked_count - 1
                WHERE id = {booking.SlotId} AND booked_count > 0
                """,
                cancellationToken);

            if (decremented != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CancelBookingResult.SlotMissingResult();
            }

            booking.Status = BookingStatus.Cancelled;
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
}
