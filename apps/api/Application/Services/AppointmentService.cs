using api.Application.Abstractions;
using api.Application.Appointments;
using api.Application.Patients;
using api.Domain;
using api.Domain.Entities;

namespace api.Application.Services;

public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IUserRepository userRepository) : IAppointmentService
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "Scheduled",
        "Completed",
        "Cancelled"
    ];

    public async Task<AppointmentDto> CreateAsync(
        CreateAppointmentRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            throw new InvalidOperationException("Patient not found.");
        }

        Guid doctorId;
        if (Roles.CanAssignAppointmentDoctor(currentRole))
        {
            if (!request.DoctorId.HasValue)
            {
                throw new InvalidOperationException("DoctorId is required.");
            }

            doctorId = request.DoctorId.Value;
            var doctor = await userRepository.GetByIdAsync(doctorId, cancellationToken);
            if (doctor is null || !string.Equals(doctor.Role, Roles.Doctor, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Doctor not found or invalid role.");
            }
        }
        else if (Roles.IsDoctor(currentRole))
        {
            doctorId = currentUserId;
        }
        else
        {
            throw new InvalidOperationException("Invalid role for creating appointments.");
        }

        var status = request.Status.Trim();
        if (!AllowedStatuses.Contains(status))
        {
            throw new InvalidOperationException("Invalid appointment status.");
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = doctorId,
            AppointmentDate = NormalizeUtc(request.AppointmentDate),
            Status = status,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        await appointmentRepository.AddAsync(appointment, cancellationToken);

        var created = await appointmentRepository.GetByIdAsync(appointment.Id, restrictToDoctorId: null, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await appointmentRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<AppointmentDto?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictDoctorId(currentUserId, currentRole);
        var appointment = await appointmentRepository.GetByIdAsync(id, restrict, cancellationToken);
        return appointment is null ? null : MapToDto(appointment);
    }

    public async Task<PagedResult<AppointmentDto>> GetPagedAsync(
        AppointmentQueryParams query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictDoctorId(currentUserId, currentRole);
        var (items, totalCount) = await appointmentRepository.GetPagedAsync(query, restrict, cancellationToken);

        return new PagedResult<AppointmentDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateAppointmentRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictDoctorId(currentUserId, currentRole);
        var appointment = await appointmentRepository.GetTrackedByIdAsync(id, restrict, cancellationToken);
        if (appointment is null)
        {
            return false;
        }

        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            throw new InvalidOperationException("Patient not found.");
        }

        Guid doctorId;
        if (Roles.CanAssignAppointmentDoctor(currentRole))
        {
            if (!request.DoctorId.HasValue)
            {
                throw new InvalidOperationException("DoctorId is required.");
            }

            doctorId = request.DoctorId.Value;
            var doctor = await userRepository.GetByIdAsync(doctorId, cancellationToken);
            if (doctor is null || !string.Equals(doctor.Role, Roles.Doctor, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Doctor not found or invalid role.");
            }
        }
        else if (Roles.IsDoctor(currentRole))
        {
            doctorId = currentUserId;
        }
        else
        {
            throw new InvalidOperationException("Invalid role for updating appointments.");
        }

        var status = request.Status.Trim();
        if (!AllowedStatuses.Contains(status))
        {
            throw new InvalidOperationException("Invalid appointment status.");
        }

        appointment.PatientId = request.PatientId;
        appointment.DoctorId = doctorId;
        appointment.AppointmentDate = NormalizeUtc(request.AppointmentDate);
        appointment.Status = status;
        appointment.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        return await appointmentRepository.UpdateAsync(appointment, cancellationToken);
    }

    private static Guid? GetRestrictDoctorId(Guid currentUserId, string currentRole)
    {
        return string.Equals(currentRole, Roles.Doctor, StringComparison.Ordinal) ? currentUserId : null;
    }

    private static AppointmentDto MapToDto(Appointment appointment)
    {
        var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim();
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patientName,
            DoctorId = appointment.DoctorId,
            DoctorName = appointment.Doctor.Username,
            AppointmentDate = appointment.AppointmentDate,
            Status = appointment.Status,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
