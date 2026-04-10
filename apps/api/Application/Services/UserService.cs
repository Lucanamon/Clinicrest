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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hash,
            Role = role
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

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
