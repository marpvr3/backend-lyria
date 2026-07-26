using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchSpecialScheduleReadService : IBranchSpecialScheduleReadService
{
    private readonly HashSet<Guid> _existingBranchIds = [];
    private BranchSpecialSchedulesResponse? _response;

    public void SeedBranchExists(Guid branchId) => _existingBranchIds.Add(branchId);

    public void SeedResponse(BranchSpecialSchedulesResponse response) => _response = response;

    public Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        bool exists = _existingBranchIds.Contains(branchId.Value);
        return Task.FromResult(exists);
    }

    public Task<BranchSpecialSchedulesResponse?> GetByDateRangeAsync(
        EstablishmentBranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
