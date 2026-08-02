using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserRestrictionRepository : IUserRestrictionRepository
{
    private readonly List<UserRestriction> _userRestrictions = [];

    public IReadOnlyList<UserRestriction> Items => _userRestrictions;

    public int SaveChangesCallCount { get; private set; }

    public void Seed(UserRestriction userRestriction) => _userRestrictions.Add(userRestriction);

    public Task<UserRestriction?> GetByIdsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        UserRestriction? found = _userRestrictions.FirstOrDefault(
            ur => ur.UserId == userId && ur.RestrictionId == restrictionId);

        return Task.FromResult(found);
    }

    public Task<bool> ExistsAsync(
        UserId userId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        bool exists = _userRestrictions.Any(
            ur => ur.UserId == userId && ur.RestrictionId == restrictionId);

        return Task.FromResult(exists);
    }

    public void Add(UserRestriction userRestriction)
    {
        _userRestrictions.Add(userRestriction);
    }

    public void Remove(UserRestriction userRestriction)
    {
        _userRestrictions.Remove(userRestriction);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
