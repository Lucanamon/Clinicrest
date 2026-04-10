using api.Application.Abstractions;
using api.Domain;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalized && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByUsernameForUpdateAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == normalized && !u.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetDoctorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Doctor && !u.IsDeleted)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Username == normalized && !u.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return false;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
