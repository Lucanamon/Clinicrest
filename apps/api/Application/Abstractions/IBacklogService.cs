using api.Application.Backlogs;
using api.Application.Patients;

namespace api.Application.Abstractions;

public interface IBacklogService
{
    Task<PagedResult<BacklogDto>> GetPagedAsync(
        BacklogQueryParams query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<BacklogDto?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<BacklogDto> CreateAsync(CreateBacklogRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid id,
        UpdateBacklogRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
