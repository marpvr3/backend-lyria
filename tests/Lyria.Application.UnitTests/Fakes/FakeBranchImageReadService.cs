using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchImages;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchImageReadService : IBranchImageReadService
{
    private readonly HashSet<EstablishmentBranchId> _existingBranches = [];
    private BranchImagesResponse? _response;

    public void SeedBranch(EstablishmentBranchId branchId) =>
        _existingBranches.Add(branchId);

    public void SetResponse(BranchImagesResponse response) =>
        _response = response;

    public Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_existingBranches.Contains(branchId));
    }

    public Task<BranchImagesResponse?> GetByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
