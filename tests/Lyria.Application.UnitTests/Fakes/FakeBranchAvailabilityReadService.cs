using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchAvailabilityReadService : IBranchAvailabilityReadService
{
    private readonly Dictionary<(Guid BranchId, DateOnly Date), BranchAvailabilityContext> _contexts = [];
    private readonly Dictionary<Guid, BranchAvailabilityContext> _batchContexts = [];

    public void SeedContext(BranchAvailabilityContext context, DateOnly date)
    {
        _contexts[(context.BranchId, date)] = context;
    }

    public void SeedBatchContext(BranchAvailabilityContext context)
    {
        _batchContexts[context.BranchId] = context;
    }

    public Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
        EstablishmentBranchId branchId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        _contexts.TryGetValue((branchId.Value, localDate), out var context);
        return Task.FromResult(context);
    }

    public Task<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>
        GetAvailabilityContextsAsync(
            IReadOnlyCollection<EstablishmentBranchId> branchIds,
            DateTimeOffset evaluatedAtUtc,
            ITimeZoneService timeZoneService,
            CancellationToken cancellationToken)
    {
        var result = new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>();

        foreach (var branchId in branchIds)
        {
            if (_batchContexts.TryGetValue(branchId.Value, out var context))
            {
                result[branchId] = context;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>(result);
    }
}
