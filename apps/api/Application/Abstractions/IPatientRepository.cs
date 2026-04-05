using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
