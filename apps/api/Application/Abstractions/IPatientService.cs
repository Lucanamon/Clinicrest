using api.Application.Patients;

namespace api.Application.Abstractions;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
