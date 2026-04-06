using api.Application.Backlogs;
using api.Domain.Entities;

namespace api.Application.Abstractions;

public interface IBacklogRepository
{
    Task<(IReadOnlyList<Backlog> Items, int TotalCount)> GetPagedAsync(
        BacklogQueryParams queryParams,
        Guid? restrictToDoctorId,
        CancellationToken cancellationToken = default);

    Task<Backlog?> GetByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default);

    Task<Backlog?> GetTrackedByIdAsync(Guid id, Guid? restrictToDoctorId, CancellationToken cancellationToken = default);

    Task AddAsync(Backlog backlog, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Backlog backlog, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
