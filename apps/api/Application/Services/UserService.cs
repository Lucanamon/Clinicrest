using api.Application.Abstractions;
using api.Application.Users;
using api.Domain;
using api.Domain.Entities;

namespace api.Application.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    private static readonly HashSet<string> CreatableRoles =
    [
        Roles.Doctor,
        Roles.Nurse,
        Roles.Administrator
    ];

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        var user = await userRepository.GetByUsernameAsync(normalized, cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        if (username.Length == 0)
        {
            throw new InvalidOperationException("Username is required.");
        }

        var role = request.Role.Trim();
        if (!CreatableRoles.Contains(role))
        {
            throw new InvalidOperationException("Role must be Doctor, Nurse, or Administrator.");
        }

        if (await userRepository.ExistsUsernameAsync(username, cancellationToken))
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var password = request.Password;
        if (password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        var profileImageUrl = request.ProfileImageUrl?.Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hash,
            Role = role,
            ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl
        };

        await userRepository.AddAsync(user, cancellationToken);
        return MapToDto(user);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (Roles.IsRootAdmin(user.Role))
        {
            throw new InvalidOperationException("The RootAdmin account cannot be deleted.");
        }

        return await userRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<UserDto?> UpdateProfileAsync(
        string username,
        UpdateProfileDto request,
        CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        var user = await userRepository.GetByUsernameForUpdateAsync(normalized, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var displayName = request.DisplayName?.Trim();
        var profileImageUrl = request.ProfileImageUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(profileImageUrl) &&
            !profileImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !profileImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid image URL. It should start with http or https.");
        }

        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        user.ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl;

        await userRepository.UpdateAsync(user, cancellationToken);
        return MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            ProfileImageUrl = user.ProfileImageUrl,
            LastActiveAt = user.LastActiveAt,
            CreatedAt = user.CreatedAt
        };
    }
}
