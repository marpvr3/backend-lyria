using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class UserRestrictionRepository(LyriaDbContext dbContext)
    : IUserRestrictionRepository
{
    public async Task<UserRestriction?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRestriction>()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.RestrictionId == restrictionId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRestriction>()
            .AnyAsync(
                x => x.UserId == userId && x.RestrictionId == restrictionId,
                cancellationToken);
    }

    public void Add(UserRestriction userRestriction)
    {
        dbContext.Set<UserRestriction>().Add(userRestriction);
    }

    public void Remove(UserRestriction userRestriction)
    {
        dbContext.Set<UserRestriction>().Remove(userRestriction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
