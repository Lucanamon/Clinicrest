using api.Application.Abstractions;
using api.Application.Backlogs;
using api.Application.Patients;
using api.Domain;
using api.Domain.Entities;

namespace api.Application.Services;

public class BacklogService(IBacklogRepository backlogRepository, IUserRepository userRepository) : IBacklogService
{
    private static readonly HashSet<string> AllowedPriorities =
    [
        "Low",
        "Medium",
        "High"
    ];

    private static readonly HashSet<string> AllowedStatuses =
    [
        "Open",
        "InProgress",
        "Done"
    ];

    public async Task<BacklogDto> CreateAsync(CreateBacklogRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAssigneeAsync(request.AssignedToUserId, cancellationToken);
        ValidatePriorityAndStatus(request.Priority, request.Status);

        var backlog = new Backlog
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Priority = request.Priority.Trim(),
            Status = request.Status.Trim(),
            AssignedToUserId = request.AssignedToUserId,
            IsDeleted = false,
            DeletedAt = null
        };

        await backlogRepository.AddAsync(backlog, cancellationToken);

        var created = await backlogRepository.GetByIdAsync(backlog.Id, restrictToDoctorId: null, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await backlogRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<BacklogDto?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictAssigneeId(currentUserId, currentRole);
        var backlog = await backlogRepository.GetByIdAsync(id, restrict, cancellationToken);
        return backlog is null ? null : MapToDto(backlog);
    }

    public async Task<PagedResult<BacklogDto>> GetPagedAsync(
        BacklogQueryParams query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictAssigneeId(currentUserId, currentRole);
        var (items, totalCount) = await backlogRepository.GetPagedAsync(query, restrict, cancellationToken);

        return new PagedResult<BacklogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateBacklogRequest request,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
        var restrict = GetRestrictAssigneeId(currentUserId, currentRole);
        var backlog = await backlogRepository.GetTrackedByIdAsync(id, restrict, cancellationToken);
        if (backlog is null)
        {
            return false;
        }

        if (Roles.IsRootAdmin(currentRole))
        {
            await ValidateAssigneeAsync(request.AssignedToUserId, cancellationToken);
        }
        else if (request.AssignedToUserId != currentUserId)
        {
            throw new InvalidOperationException("Only RootAdmin may reassign a backlog item to another user.");
        }

        ValidatePriorityAndStatus(request.Priority, request.Status);

        backlog.Title = request.Title.Trim();
        backlog.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        backlog.Priority = request.Priority.Trim();
        backlog.Status = request.Status.Trim();
        backlog.AssignedToUserId = request.AssignedToUserId;

        return await backlogRepository.UpdateAsync(backlog, cancellationToken);
    }

    private static Guid? GetRestrictAssigneeId(Guid currentUserId, string currentRole)
    {
        if (Roles.IsRootAdmin(currentRole))
        {
            return null;
        }

        return currentUserId;
    }

    private async Task ValidateAssigneeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Assigned user not found.");
        }
    }

    private static void ValidatePriorityAndStatus(string priority, string status)
    {
        var p = priority.Trim();
        var s = status.Trim();
        if (!AllowedPriorities.Contains(p))
        {
            throw new InvalidOperationException("Invalid priority.");
        }

        if (!AllowedStatuses.Contains(s))
        {
            throw new InvalidOperationException("Invalid backlog status.");
        }
    }

    private static BacklogDto MapToDto(Backlog backlog)
    {
        return new BacklogDto
        {
            Id = backlog.Id,
            Title = backlog.Title,
            Description = backlog.Description,
            Priority = backlog.Priority,
            Status = backlog.Status,
            AssignedToUserId = backlog.AssignedToUserId,
            AssignedToName = backlog.AssignedTo.Username,
            CreatedAt = backlog.CreatedAt
        };
    }
}
