using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchAvailabilityReadService : IBranchAvailabilityReadService
{
    private readonly Dictionary<(Guid BranchId, DateOnly Date), BranchAvailabilityContext> _contexts = [];

    public void SeedContext(BranchAvailabilityContext context, DateOnly date)
    {
        _contexts[(context.BranchId, date)] = context;
    }

    public Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
        EstablishmentBranchId branchId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        _contexts.TryGetValue((branchId.Value, localDate), out var context);
        return Task.FromResult(context);
    }
}
