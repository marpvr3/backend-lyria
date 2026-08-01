using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(LyriaDbContext dbContext)
    : IUserRepository
{
    public async Task<User?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        UserId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<User>()
            .AnyAsync(
                u => u.Email == normalizedEmail &&
                     (excludingId == null || u.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<User>()
            .AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
