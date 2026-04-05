using api.Application.Patients;

namespace api.Application.Abstractions;

public interface IPatientService
{
    Task<PagedResult<PatientDto>> GetPagedAsync(
        PatientQueryParams query,
        CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
