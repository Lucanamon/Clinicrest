using api.Application.Patients;
using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface IPatientRepository
{
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
        PatientQueryParams query,
        CancellationToken cancellationToken = default);

    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Patient>> GetForExportAsync(
        PatientExportRequest request,
        CancellationToken cancellationToken = default);

    Task<(int TotalPatients, int ActivePatients, int DeletedPatients)> GetSummaryCountsAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
