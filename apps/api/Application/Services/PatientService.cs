using api.Application.Abstractions;
using api.Application.Patients;
using api.Domain.Entities;

namespace api.Application.Services;

public class PatientService(IPatientRepository repository) : IPatientService
{
    public async Task<PatientDto> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Date, DateTimeKind.Utc),
            Gender = request.Gender.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            CreatedAt = utcNow,
            IsDeleted = false,
            DeletedAt = null
        };

        await repository.AddAsync(patient, cancellationToken);
        return MapToDto(patient);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<PagedResult<PatientDto>> GetPagedAsync(
        PatientQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<PatientDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null ? null : MapToDto(patient);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return false;
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Date, DateTimeKind.Utc);
        patient.Gender = request.Gender.Trim();
        patient.PhoneNumber = request.PhoneNumber.Trim();

        return await repository.UpdateAsync(patient, cancellationToken);
    }

    private static PatientDto MapToDto(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            CreatedAt = patient.CreatedAt
        };
    }
}
