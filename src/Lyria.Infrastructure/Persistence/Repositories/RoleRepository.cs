using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(LyriaDbContext dbContext)
    : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        RoleId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Role>()
            .AnyAsync(
                r => r.Code == normalizedCode &&
                     (excludingId == null || r.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Role role,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<Role>()
            .AddAsync(role, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
