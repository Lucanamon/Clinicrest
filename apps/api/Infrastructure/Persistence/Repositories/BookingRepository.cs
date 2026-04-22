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
    private const string DuplicateBookingError = "DuplicateBooking";
    private const string ReminderMessageText = "Reminder: your appointment is coming soon";
    private const string MissingEmailPlaceholder = "no-patient-email@clinicrest.invalid";

    public async Task<BookingResult> CreateAsync(
        long slotId,
        string patientName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateSingleAttemptAsync(slotId, patientName, phoneNumber, cancellationToken);
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
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var normalizedPatientName = patientName.Trim();
            var normalizedPhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedPhoneNumber))
            {
                var hasDuplicate = await dbContext.Bookings.AsNoTracking().AnyAsync(
                    booking =>
                        booking.Status != BookingStatus.Cancelled &&
                        booking.PatientName.ToLower() == normalizedPatientName.ToLower() &&
                        booking.PhoneNumber != null &&
                        booking.PhoneNumber.ToLower() == normalizedPhoneNumber.ToLower(),
                    cancellationToken);

                if (hasDuplicate)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failed(DuplicateBookingError);
                }
            }

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
                PatientName = normalizedPatientName,
                PhoneNumber = normalizedPhoneNumber,
                Status = BookingStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync(cancellationToken);
            await TryAddPendingNotificationJobAsync(booking, cancellationToken);
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
            await CancelPendingNotificationJobsAsync(booking.Id, cancellationToken);
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

    public async Task<bool> ScheduleAsync(long bookingId, Guid patientId, CancellationToken cancellationToken = default)
    {
        await EnsureBookingPatientIdColumnAsync(cancellationToken);

        var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Active)
        {
            return false;
        }

        booking.PatientId = patientId;
        booking.Status = BookingStatus.Scheduled;
        await SyncNotificationJobAfterScheduleAsync(booking, patientId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RescheduleBookingResult> RescheduleAsync(
        long bookingId,
        long newSlotId,
        CancellationToken cancellationToken = default)
    {
        if (newSlotId <= 0)
        {
            return RescheduleBookingResult.Fail("new_slot_id is required.");
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await RescheduleSingleAttemptAsync(bookingId, newSlotId, cancellationToken);
            }
            catch (Exception ex) when (IsTransientPostgres(ex) && attempt < MaxSerializationRetries)
            {
                dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(25 * (1 << attempt)), cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<BookingListItem>> GetListByStatusAsync(
        BookingStatus status,
        CancellationToken cancellationToken = default)
    {
        await EnsureBookingPatientIdColumnAsync(cancellationToken);

        var list = await (
            from booking in dbContext.Bookings.AsNoTracking()
            join slot in dbContext.Slots.AsNoTracking() on booking.SlotId equals slot.Id into slots
            from slot in slots.DefaultIfEmpty()
            join patient in dbContext.Patients.AsNoTracking() on booking.PatientId equals patient.Id into patients
            from patient in patients.DefaultIfEmpty()
            join doctor in dbContext.Users.AsNoTracking() on booking.DoctorId equals doctor.Id into doctors
            from doctor in doctors.DefaultIfEmpty()
            where booking.Status == status
            orderby booking.CreatedAt descending
            select new BookingListItem
            {
                Id = booking.Id,
                SlotId = booking.SlotId,
                PatientName = booking.PatientName ?? "Guest",
                PhoneNumber = patient != null && !string.IsNullOrWhiteSpace(patient.PhoneNumber)
                    ? patient.PhoneNumber
                    : booking.PhoneNumber,
                PatientId = booking.PatientId,
                DoctorName = doctor != null ? doctor.Username : null,
                Status = booking.Status == BookingStatus.Scheduled
                    ? "SCHEDULED"
                    : booking.Status == BookingStatus.Cancelled
                        ? "CANCELLED"
                        : "ACTIVE",
                CreatedAt = booking.CreatedAt,
                SlotStartTime = slot != null ? slot.StartTime : null
            }).ToListAsync(cancellationToken);

        if (list.Count == 0)
        {
            return list;
        }

        var bookingIds = list.Select(b => b.Id).ToList();
        var allJobs = await dbContext.NotificationJobs
            .AsNoTracking()
            .Where(n => bookingIds.Contains(n.BookingId))
            .ToListAsync(cancellationToken);

        var latestByBooking = allJobs
            .GroupBy(n => n.BookingId)
            .ToDictionary(
                g => g.Key,
                g => PickRepresentativeJob(g.ToList()));

        foreach (var item in list)
        {
            if (!latestByBooking.TryGetValue(item.Id, out var job))
            {
                continue;
            }

            item.NotificationStatus = MapNotificationStatusForApi(job.Status);
            item.LastError = job.ErrorMessage;
        }

        return list;
    }

    public async Task<bool> ResetFailedNotificationForRetryAsync(
        long bookingId,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.NotificationJobs
            .Where(j => j.BookingId == bookingId && j.Status == NotificationStatus.Failed)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return false;
        }

        job.Status = NotificationStatus.Pending;
        job.RetryCount = 0;
        job.ErrorMessage = null;
        var now = DateTime.UtcNow;
        job.ScheduledSendTime = now;
        job.NextAttemptAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? MapNotificationStatusForApi(NotificationStatus status) =>
        status switch
        {
            NotificationStatus.Pending => "Pending",
            NotificationStatus.Sent => "Sent",
            NotificationStatus.Failed => "Failed",
            NotificationStatus.Retrying => "Retrying",
            NotificationStatus.Cancelled => "Cancelled",
            _ => null
        };

    private async Task EnsureBookingPatientIdColumnAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE bookings
            ADD COLUMN IF NOT EXISTS patient_id uuid;
            """,
            cancellationToken);
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

    private static DateTime ComputeScheduledSendUtc(DateTime appointmentStart) =>
        NormalizeToUtc(appointmentStart).AddHours(-1);

    private async Task TryAddPendingNotificationJobAsync(Booking booking, CancellationToken cancellationToken)
    {
        var slot = await dbContext.Slots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == booking.SlotId, cancellationToken);
        if (slot is null)
        {
            return;
        }

        var scheduledSend = ComputeScheduledSendUtc(slot.StartTime);
        if (scheduledSend <= DateTime.UtcNow)
        {
            return;
        }

        var createdAt = DateTime.UtcNow;
        var phone = booking.PhoneNumber ?? string.Empty;
        dbContext.NotificationJobs.Add(CreateSmsJob(booking.Id, booking.PatientName, phone, scheduledSend, createdAt));
        dbContext.NotificationJobs.Add(CreateEmailJob(booking.Id, booking.PatientName, null, scheduledSend, createdAt));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CancelPendingNotificationJobsAsync(long bookingId, CancellationToken cancellationToken)
    {
        var jobs = await dbContext.NotificationJobs
            .Where(j =>
                j.BookingId == bookingId &&
                (j.Status == NotificationStatus.Pending || j.Status == NotificationStatus.Retrying))
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            job.Status = NotificationStatus.Cancelled;
        }
    }

    private async Task SyncNotificationJobAfterScheduleAsync(
        Booking booking,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var slot = await dbContext.Slots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == booking.SlotId, cancellationToken);
        if (slot is null)
        {
            return;
        }

        var patient = await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        var patientName = patient is not null
            ? $"{patient.FirstName} {patient.LastName}".Trim()
            : booking.PatientName;

        var phone = patient?.PhoneNumber ?? booking.PhoneNumber ?? string.Empty;
        var scheduledSend = ComputeScheduledSendUtc(slot.StartTime);
        var utcNow = DateTime.UtcNow;

        var pendingJobs = await dbContext.NotificationJobs
            .Where(j => j.BookingId == booking.Id && j.Status == NotificationStatus.Pending)
            .ToListAsync(cancellationToken);

        if (scheduledSend <= utcNow)
        {
            foreach (var job in pendingJobs)
            {
                job.Status = NotificationStatus.Cancelled;
            }

            return;
        }

        if (pendingJobs.Count == 0)
        {
            dbContext.NotificationJobs.Add(CreateSmsJob(booking.Id, patientName, phone, scheduledSend, utcNow));
            dbContext.NotificationJobs.Add(CreateEmailJob(booking.Id, patientName, ResolvePatientEmail(patient), scheduledSend, utcNow));
            return;
        }

        var emailForChannel = ResolvePatientEmail(patient);
        foreach (var job in pendingJobs)
        {
            job.PatientName = patientName;
            job.ScheduledSendTime = scheduledSend;
            job.NextAttemptAt = scheduledSend;
            job.Message = ReminderMessageText;
            if (job.Channel == NotificationChannel.Sms)
            {
                job.PhoneNumber = phone;
            }
            else
            {
                job.EmailAddress = string.IsNullOrWhiteSpace(emailForChannel)
                    ? MissingEmailPlaceholder
                    : emailForChannel.Trim();
            }
        }

        EnsureSmsAndEmailJobsExist(
            booking.Id,
            patientName,
            phone,
            scheduledSend,
            utcNow,
            emailForChannel,
            pendingJobs);
    }

    private async Task ApplyPendingNotificationJobAfterSlotChangeAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        var slot = await dbContext.Slots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == booking.SlotId, cancellationToken);
        if (slot is null)
        {
            return;
        }

        var scheduledSend = ComputeScheduledSendUtc(slot.StartTime);
        var utcNow = DateTime.UtcNow;

        var pendingJobs = await dbContext.NotificationJobs
            .Where(j => j.BookingId == booking.Id && j.Status == NotificationStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingJobs.Count == 0)
        {
            if (scheduledSend > utcNow)
            {
                var phone = booking.PhoneNumber ?? string.Empty;
                dbContext.NotificationJobs.Add(CreateSmsJob(booking.Id, booking.PatientName, phone, scheduledSend, utcNow));
                dbContext.NotificationJobs.Add(CreateEmailJob(booking.Id, booking.PatientName, null, scheduledSend, utcNow));
            }

            return;
        }

        if (scheduledSend <= utcNow)
        {
            foreach (var job in pendingJobs)
            {
                job.Status = NotificationStatus.Cancelled;
            }

            return;
        }

        foreach (var job in pendingJobs)
        {
            job.ScheduledSendTime = scheduledSend;
            job.NextAttemptAt = scheduledSend;
            job.Message = ReminderMessageText;
            if (job.Channel == NotificationChannel.Sms)
            {
                job.PhoneNumber = booking.PhoneNumber ?? string.Empty;
            }
            else
            {
                job.EmailAddress = MissingEmailPlaceholder;
            }
        }

        EnsureSmsAndEmailJobsExist(
            booking.Id,
            booking.PatientName,
            booking.PhoneNumber ?? string.Empty,
            scheduledSend,
            utcNow,
            patientEmail: null,
            pendingJobs);
    }

    private async Task<RescheduleBookingResult> RescheduleSingleAttemptAsync(
        long bookingId,
        long newSlotId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RescheduleBookingResult.NotFoundResult();
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RescheduleBookingResult.Fail("Cannot reschedule a cancelled booking.");
            }

            if (booking.SlotId == newSlotId)
            {
                await transaction.CommitAsync(cancellationToken);
                return RescheduleBookingResult.SuccessResult();
            }

            var oldSlotId = booking.SlotId;

            var released = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE slots
                 SET booked_count = booked_count - 1
                 WHERE id = {oldSlotId} AND booked_count > 0
                 """,
                cancellationToken);

            if (released != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RescheduleBookingResult.Fail("Could not release the previous slot.");
            }

            var reserved = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE slots
                 SET booked_count = booked_count + 1
                 WHERE id = {newSlotId}
                   AND booked_count < capacity
                   AND start_time > NOW()
                 """,
                cancellationToken);

            if (reserved != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RescheduleBookingResult.Fail("Slot full, invalid, or not in the future.");
            }

            booking.SlotId = newSlotId;
            await ApplyPendingNotificationJobAfterSlotChangeAsync(booking, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return RescheduleBookingResult.SuccessResult();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string? ResolvePatientEmail(Patient? patient) =>
        // Patient has no email field today; when added, return it from patient here.
        null;

    private void EnsureSmsAndEmailJobsExist(
        long bookingId,
        string patientName,
        string phone,
        DateTime scheduledSend,
        DateTime createdAt,
        string? patientEmail,
        List<NotificationJob> alreadyPending)
    {
        if (!alreadyPending.Exists(j => j.Channel == NotificationChannel.Sms))
        {
            dbContext.NotificationJobs.Add(CreateSmsJob(bookingId, patientName, phone, scheduledSend, createdAt));
        }

        if (!alreadyPending.Exists(j => j.Channel == NotificationChannel.Email))
        {
            dbContext.NotificationJobs.Add(CreateEmailJob(bookingId, patientName, patientEmail, scheduledSend, createdAt));
        }
    }

    private static int NotificationStatusSortKey(NotificationStatus status) =>
        status switch
        {
            NotificationStatus.Failed => 0,
            NotificationStatus.Retrying => 1,
            NotificationStatus.Pending => 2,
            NotificationStatus.Sent => 3,
            NotificationStatus.Cancelled => 4,
            _ => 5
        };

    private static NotificationJob PickRepresentativeJob(IReadOnlyList<NotificationJob> jobs) =>
        jobs
            .OrderBy(j => NotificationStatusSortKey(j.Status))
            .ThenByDescending(j => j.CreatedAt)
            .ThenBy(j => j.Id)
            .First();

    private static NotificationJob CreateSmsJob(
        long bookingId,
        string patientName,
        string phone,
        DateTime scheduledSend,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PatientName = patientName,
            PhoneNumber = phone,
            Message = ReminderMessageText,
            ScheduledSendTime = scheduledSend,
            NextAttemptAt = scheduledSend,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            Channel = NotificationChannel.Sms,
            CreatedAt = createdAt
        };

    private static NotificationJob CreateEmailJob(
        long bookingId,
        string patientName,
        string? emailAddress,
        DateTime scheduledSend,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PatientName = patientName,
            PhoneNumber = string.Empty,
            EmailAddress = string.IsNullOrWhiteSpace(emailAddress) ? MissingEmailPlaceholder : emailAddress.Trim(),
            Message = ReminderMessageText,
            ScheduledSendTime = scheduledSend,
            NextAttemptAt = scheduledSend,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            Channel = NotificationChannel.Email,
            CreatedAt = createdAt
        };

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
