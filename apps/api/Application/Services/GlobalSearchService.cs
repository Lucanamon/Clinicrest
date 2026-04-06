using api.Application.Abstractions;
using api.Application.Appointments;
using api.Application.Backlogs;
using api.Application.Patients;
using api.Application.Search;
using api.Domain;
using api.Domain.Entities;
using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.Application.Services;

public class GlobalSearchService(ApplicationDbContext dbContext) : IGlobalSearchService
{
    private const int MaxPerCategory = 5;

    public async Task<GlobalSearchResult> SearchAsync(
        string query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var term = SanitizeLikeTerm(query);
        if (term.Length == 0)
        {
            return new GlobalSearchResult();
        }

        var pattern = $"%{term}%";
        var restrictDoctorId = Roles.IsDoctor(currentRole)
            ? currentUserId
            : (Guid?)null;
        Guid? restrictBacklogAssigneeId = Roles.IsRootAdmin(currentRole)
            ? null
            : currentUserId;

        var patients = await dbContext.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                (EF.Functions.ILike(p.FirstName, pattern) || EF.Functions.ILike(p.LastName, pattern)))
            .OrderByDescending(p => p.CreatedAt)
            .Take(MaxPerCategory)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                PhoneNumber = p.PhoneNumber,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var appointmentsQuery = dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => !a.IsDeleted && !a.Patient.IsDeleted &&
                (EF.Functions.ILike(a.Patient.FirstName, pattern) ||
                 EF.Functions.ILike(a.Patient.LastName, pattern) ||
                 (a.Notes != null && EF.Functions.ILike(a.Notes, pattern))));

        if (restrictDoctorId.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == restrictDoctorId.Value);
        }

        var appointmentEntities = await appointmentsQuery
            .OrderByDescending(a => a.AppointmentDate)
            .Take(MaxPerCategory)
            .ToListAsync(cancellationToken);

        var appointments = appointmentEntities.Select(MapAppointmentToDto).ToList();

        var backlogsQuery = dbContext.Backlogs
            .AsNoTracking()
            .Include(b => b.AssignedTo)
            .Where(b => !b.IsDeleted &&
                (EF.Functions.ILike(b.Title, pattern) ||
                 (b.Description != null && EF.Functions.ILike(b.Description, pattern))));

        if (restrictBacklogAssigneeId.HasValue)
        {
            backlogsQuery = backlogsQuery.Where(b => b.AssignedToUserId == restrictBacklogAssigneeId.Value);
        }

        var backlogEntities = await backlogsQuery
            .OrderByDescending(b => b.CreatedAt)
            .Take(MaxPerCategory)
            .ToListAsync(cancellationToken);

        var backlogs = backlogEntities.Select(MapBacklogToDto).ToList();

        return new GlobalSearchResult
        {
            Patients = patients,
            Appointments = appointments,
            Backlogs = backlogs
        };
    }

    private static AppointmentDto MapAppointmentToDto(Appointment a)
    {
        var patientName = $"{a.Patient.FirstName} {a.Patient.LastName}".Trim();
        return new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = patientName,
            DoctorId = a.DoctorId,
            DoctorName = a.Doctor.Username,
            AppointmentDate = a.AppointmentDate,
            Status = a.Status,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        };
    }

    private static BacklogDto MapBacklogToDto(Backlog b)
    {
        return new BacklogDto
        {
            Id = b.Id,
            Title = b.Title,
            Description = b.Description,
            Priority = b.Priority,
            Status = b.Status,
            AssignedToUserId = b.AssignedToUserId,
            AssignedToName = b.AssignedTo.Username,
            CreatedAt = b.CreatedAt
        };
    }

    private static string SanitizeLikeTerm(string value)
    {
        return value.Trim().Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
