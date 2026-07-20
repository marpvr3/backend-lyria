using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeRestrictionRepository : IRestrictionRepository
{
    private readonly List<Restriction> _restrictions = [];

    public int SaveChangesCallCount { get; private set; }

    public void Seed(Restriction restriction) => _restrictions.Add(restriction);

    public Task<Restriction?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id));
    }

    public Task<bool> ExistsByNameAsync(
        string normalizedName,
        RestrictionId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _restrictions.Any(r =>
            string.Equals(r.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            (excludingId is null || r.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Restriction restriction,
        CancellationToken cancellationToken)
    {
        _restrictions.Add(restriction);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
