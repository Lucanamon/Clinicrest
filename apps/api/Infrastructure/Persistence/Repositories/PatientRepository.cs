using api.Application.Abstractions;
using api.Application.Patients;
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
        var patient = await dbContext.Patients.FirstOrDefaultAsync(
            p => p.Id == id && !p.IsDeleted,
            cancellationToken);
        if (patient is null)
        {
            return false;
        }

        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(
        PatientQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Patient> query = dbContext.Patients.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = SanitizeLikeTerm(queryParams.SearchTerm);
            if (term.Length > 0)
            {
                var pattern = $"%{term}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.FirstName, pattern) ||
                    EF.Functions.ILike(p.LastName, pattern));
            }
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Gender))
        {
            var gender = queryParams.Gender.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Gender, gender));
        }

        if (queryParams.FromDateOfBirth.HasValue)
        {
            var from = DateTime.SpecifyKind(queryParams.FromDateOfBirth.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.DateOfBirth >= from);
        }

        if (queryParams.ToDateOfBirth.HasValue)
        {
            var to = DateTime.SpecifyKind(queryParams.ToDateOfBirth.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.DateOfBirth <= to);
        }

        query = ApplyOrdering(query, queryParams);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = queryParams.PageNumber;
        var pageSize = queryParams.PageSize;
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients.FirstOrDefaultAsync(
            p => p.Id == id && !p.IsDeleted,
            cancellationToken);
    }

    public async Task<bool> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static readonly HashSet<string> AllowedSortProperties =
    [
        nameof(Patient.FirstName),
        nameof(Patient.LastName),
        nameof(Patient.CreatedAt),
        nameof(Patient.DateOfBirth),
        nameof(Patient.Gender),
        nameof(Patient.PhoneNumber)
    ];

    private static IQueryable<Patient> ApplyOrdering(IQueryable<Patient> queryable, PatientQueryParams queryParams)
    {
        if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
        {
            var sortBy = queryParams.SortBy.Trim();
            if (AllowedSortProperties.Contains(sortBy))
            {
                if (string.Equals(queryParams.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    return queryable.OrderByDescending(x => EF.Property<object>(x, sortBy));
                }

                return queryable.OrderBy(x => EF.Property<object>(x, sortBy));
            }
        }

        return queryable.OrderByDescending(x => x.CreatedAt);
    }

    /// <summary>
    /// Strips ILIKE wildcards so user input cannot broaden the pattern.
    /// </summary>
    private static string SanitizeLikeTerm(string value)
    {
        return value.Trim().Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
