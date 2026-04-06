using api.Application.Search;

namespace api.Application.Abstractions;

public interface IGlobalSearchService
{
    Task<GlobalSearchResult> SearchAsync(
        string query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);
}
