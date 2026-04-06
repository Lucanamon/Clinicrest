using api.Application.Abstractions;
using api.Domain;
using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetDoctorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Doctor)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }
}
