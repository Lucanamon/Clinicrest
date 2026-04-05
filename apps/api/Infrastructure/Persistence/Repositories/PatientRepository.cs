using api.Application.Abstractions;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class PatientRepository(ApplicationDbContext dbContext) : IPatientRepository
{
    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient is null)
        {
            return false;
        }

        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .AsNoTracking()
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
