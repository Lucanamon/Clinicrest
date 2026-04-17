using api.Application.Abstractions;
using api.Application.Appointments;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace api.Infrastructure.Persistence.Repositories;

public class AppointmentRepository(ApplicationDbContext dbContext) : IAppointmentRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments.FirstOrDefaultAsync(
            a => a.Id == id && !a.IsDeleted,
            cancellationToken);
        if (appointment is null)
        {
            return false;
        }

        dbContext.Appointments.Remove(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        AppointmentQueryParams queryParams,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => !a.IsDeleted && !a.Patient.IsDeleted && !a.Doctor.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == restrictToDoctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = SanitizeLikeTerm(queryParams.SearchTerm);
            if (term.Length > 0)
            {
                var pattern = $"%{term}%";
                query = query.Where(a =>
                    EF.Functions.ILike(a.Patient.FirstName, pattern) ||
                    EF.Functions.ILike(a.Patient.LastName, pattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            var status = queryParams.Status.Trim();
            query = query.Where(a => EF.Functions.ILike(a.Status, status));
        }

        if (queryParams.FromAppointmentDate.HasValue)
        {
            var from = DateTime.SpecifyKind(queryParams.FromAppointmentDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.AppointmentDate >= from);
        }

        if (queryParams.ToAppointmentDate.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(queryParams.ToAppointmentDate.Value.Date, DateTimeKind.Utc)
                .AddDays(1);
            query = query.Where(a => a.AppointmentDate < toExclusive);
        }

        query = ApplyOrdering(query, queryParams);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = queryParams.PageNumber;
        var pageSize = queryParams.PageSize;
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default)
    {
        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Id == id && !a.IsDeleted && !a.Patient.IsDeleted && !a.Doctor.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == restrictToDoctorId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Appointment?> GetTrackedByIdAsync(
        Guid id,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Appointment> query = dbContext.Appointments
            .Where(a => a.Id == id && !a.IsDeleted && !a.Doctor.IsDeleted);

        if (restrictToDoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == restrictToDoctorId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Appointment?> FinalizeFromBookingAsync(
        long bookingId,
        Guid patientId,
        Guid doctorId,
        DateTime appointmentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE bookings
            ADD COLUMN IF NOT EXISTS patient_id uuid;
            ALTER TABLE bookings
            ADD COLUMN IF NOT EXISTS doctor_id uuid;
            """,
            cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking is null || booking.Status != BookingStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            booking.PatientId = patientId;
            booking.DoctorId = doctorId;
            booking.Status = BookingStatus.Scheduled;

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = appointmentDate,
                Status = "Scheduled",
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            dbContext.Appointments.Add(appointment);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await dbContext.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointment.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static readonly HashSet<string> AllowedSortProperties =
    [
        nameof(Appointment.AppointmentDate),
        nameof(Appointment.CreatedAt),
        nameof(Appointment.Status)
    ];

    private static IQueryable<Appointment> ApplyOrdering(IQueryable<Appointment> queryable, AppointmentQueryParams queryParams)
    {
        if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
        {
            var sortBy = queryParams.SortBy.Trim();
            if (AllowedSortProperties.Contains(sortBy))
            {
                if (string.Equals(queryParams.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    return queryable.OrderByDescending(x => EF.Property<object>(x, sortBy));
                }

                return queryable.OrderBy(x => EF.Property<object>(x, sortBy));
            }
        }

        return queryable.OrderByDescending(x => x.AppointmentDate);
    }

    private static string SanitizeLikeTerm(string value)
    {
        return value.Trim().Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
