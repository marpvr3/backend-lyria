using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakePublicBranchReadService : IPublicBranchReadService
{
    private readonly Dictionary<Guid, PublicBranchFullDetailResponse> _branches = new();

    public void Seed(Guid branchId, PublicBranchFullDetailResponse response)
    {
        _branches[branchId] = response;
    }

    public Task<PublicBranchFullDetailResponse?> GetByIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        _branches.TryGetValue(branchId.Value, out var result);
        return Task.FromResult(result);
    }
}
