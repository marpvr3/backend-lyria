using Lyria.Application.Features.UserRestrictions;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRestrictionReadService
{
    Task<IReadOnlyList<UserRestrictionResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<UserRestrictionResponse?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);
}
