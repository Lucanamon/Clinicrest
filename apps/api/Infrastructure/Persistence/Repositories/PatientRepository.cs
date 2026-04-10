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

        dbContext.Patients.Remove(patient);
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
            if (string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => EF.Functions.ILike(p.Gender, "Male"));
            }
            else if (string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => EF.Functions.ILike(p.Gender, "Female"));
            }
            else if (string.Equals(gender, "Other", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p =>
                    p.Gender != null &&
                    !EF.Functions.ILike(p.Gender, "Male") &&
                    !EF.Functions.ILike(p.Gender, "Female"));
            }
            else
            {
                query = query.Where(p => EF.Functions.ILike(p.Gender, gender));
            }
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

    public async Task<IReadOnlyList<Patient>> GetForExportAsync(
        PatientExportRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Patient> query = dbContext.Patients.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = SanitizeLikeTerm(request.Name);
            if (term.Length > 0)
            {
                var pattern = $"%{term}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.FirstName, pattern) ||
                    EF.Functions.ILike(p.LastName, pattern));
            }
        }

        query = ApplyExportOrdering(query, request.SortBy);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(int TotalPatients, int ActivePatients, int DeletedPatients)> GetSummaryCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalPatients = await dbContext.Patients.AsNoTracking().CountAsync(cancellationToken);
        var deletedPatients = await dbContext.Patients.AsNoTracking()
            .CountAsync(p => p.IsDeleted, cancellationToken);
        var activePatients = totalPatients - deletedPatients;

        return (totalPatients, activePatients, deletedPatients);
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

    private static IQueryable<Patient> ApplyOrdering(IQueryable<Patient> queryable, PatientQueryParams queryParams)
    {
        if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
        {
            var sortBy = queryParams.SortBy.Trim();
            var desc = string.Equals(queryParams.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase))
            {
                return desc
                    ? queryable.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName)
                    : queryable.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
            }

            if (string.Equals(sortBy, "createdAt", StringComparison.OrdinalIgnoreCase))
            {
                return desc
                    ? queryable.OrderByDescending(x => x.CreatedAt)
                    : queryable.OrderBy(x => x.CreatedAt);
            }
        }

        return queryable.OrderByDescending(x => x.CreatedAt);
    }

    private static IQueryable<Patient> ApplyExportOrdering(IQueryable<Patient> queryable, string? sortBy)
    {
        if (string.Equals(sortBy?.Trim(), "name", StringComparison.OrdinalIgnoreCase))
        {
            return queryable.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
        }

        if (string.Equals(sortBy?.Trim(), "createdAt", StringComparison.OrdinalIgnoreCase))
        {
            return queryable.OrderByDescending(x => x.CreatedAt);
        }

        return queryable.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
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
