using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchServiceRepository : IEstablishmentBranchServiceRepository
{
    private readonly List<EstablishmentBranchService> _items = [];

    public int SaveChangesCallCount { get; private set; }

    public void Seed(EstablishmentBranchService item) => _items.Add(item);

    public Task<EstablishmentBranchService?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(
            x => x.BranchId == branchId && x.ServiceId == serviceId));
    }

    public Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.Any(
            x => x.BranchId == branchId && x.ServiceId == serviceId));
    }

    public void Add(EstablishmentBranchService branchService)
    {
        _items.Add(branchService);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
