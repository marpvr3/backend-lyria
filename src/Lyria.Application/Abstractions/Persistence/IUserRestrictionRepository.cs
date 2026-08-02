using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRestrictionRepository
{
    Task<UserRestriction?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);

    void Add(UserRestriction userRestriction);

    void Remove(UserRestriction userRestriction);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
