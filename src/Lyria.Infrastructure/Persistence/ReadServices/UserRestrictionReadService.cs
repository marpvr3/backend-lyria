using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class UserRestrictionReadService(LyriaDbContext dbContext)
    : IUserRestrictionReadService
{
    public async Task<IReadOnlyList<UserRestrictionResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRestriction>()
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking(),
                ur => ur.RestrictionId,
                r => r.Id,
                (ur, r) => new UserRestrictionResponse(
                    ur.UserId.Value,
                    ur.RestrictionId.Value,
                    r.Name,
                    ur.ImportanceLevel,
                    ur.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRestrictionResponse?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRestriction>()
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.RestrictionId == restrictionId)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking(),
                ur => ur.RestrictionId,
                r => r.Id,
                (ur, r) => new UserRestrictionResponse(
                    ur.UserId.Value,
                    ur.RestrictionId.Value,
                    r.Name,
                    ur.ImportanceLevel,
                    ur.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
