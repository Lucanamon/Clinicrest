using api.Application.Appointments;
using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface IAppointmentRepository
{
    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        AppointmentQueryParams queryParams,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default);

    Task<Appointment?> GetByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default);

    Task<Appointment?> GetTrackedByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Appointment?> FinalizeFromBookingAsync(
        long bookingId,
        Guid patientId,
        Guid doctorId,
        DateTime appointmentDate,
        string? phoneNumber,
        string? notes,
        CancellationToken cancellationToken = default);
}
