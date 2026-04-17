using api.Application.Appointments;
using api.Application.Patients;

namespace api.Application.Abstractions;

public interface IAppointmentService
{
    Task<PagedResult<AppointmentDto>> GetPagedAsync(
        AppointmentQueryParams query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<AppointmentDto?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<AppointmentDto> CreateAsync(
        CreateAppointmentRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateAppointmentRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppointmentDto?> FinalizeAsync(
        FinalizeAppointmentRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);
}
