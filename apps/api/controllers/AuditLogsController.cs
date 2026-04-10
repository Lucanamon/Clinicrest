using api.Application.Abstractions;
using api.Application.AuditLogs;
using api.Application.Patients;
using api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = Roles.RootAdmin)]
public class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogListItemDto>>> GetAuditLogs(
        [FromQuery] AuditLogQueryParams query,
        CancellationToken cancellationToken = default)
    {
        if (query.PageNumber < 1)
        {
            query.PageNumber = 1;
        }

        if (query.PageSize < 1)
        {
            query.PageSize = 20;
        }

        if (query.PageSize > 200)
        {
            query.PageSize = 200;
        }

        var result = await auditLogService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }
}
