using api.Application.AuditLogs;
using api.Application.Patients;

namespace api.Application.Abstractions;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogListItemDto>> GetPagedAsync(
        AuditLogQueryParams query,
        CancellationToken cancellationToken = default);
}
