using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchRestrictionRepository : IEstablishmentBranchRestrictionRepository
{
    private readonly List<EstablishmentBranchRestriction> _items = [];

    public int SaveChangesCallCount { get; private set; }

    public void Seed(EstablishmentBranchRestriction item) => _items.Add(item);

    public Task<EstablishmentBranchRestriction?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(
            x => x.BranchId == branchId && x.RestrictionId == restrictionId));
    }

    public Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.Any(
            x => x.BranchId == branchId && x.RestrictionId == restrictionId));
    }

    public void Add(EstablishmentBranchRestriction branchRestriction)
    {
        _items.Add(branchRestriction);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
