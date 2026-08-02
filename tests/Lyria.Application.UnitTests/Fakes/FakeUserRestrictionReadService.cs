using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserRestrictionReadService : IUserRestrictionReadService
{
    private readonly List<UserRestrictionResponse> _responses = [];

    public void Seed(UserRestrictionResponse response) => _responses.Add(response);

    public Task<IReadOnlyList<UserRestrictionResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserRestrictionResponse> found = _responses
            .Where(r => r.UserId == userId.Value)
            .ToList();

        return Task.FromResult(found);
    }

    public Task<UserRestrictionResponse?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        UserRestrictionResponse? found = _responses.FirstOrDefault(
            r => r.UserId == userId.Value && r.RestrictionId == restrictionId.Value);

        return Task.FromResult(found);
    }
}
