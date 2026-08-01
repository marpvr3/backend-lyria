using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class UserRoleRepository(LyriaDbContext dbContext)
    : IUserRoleRepository
{
    public async Task<UserRole?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        UserRole userRole,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<UserRole>()
            .AddAsync(userRole, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
