using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchScheduleReadService : IBranchScheduleReadService
{
    private readonly HashSet<EstablishmentBranchId> _existingBranches = [];
    private BranchWeeklyScheduleResponse? _weeklyResponse;
    private BranchDayScheduleResponse? _dayResponse;

    public void SeedBranch(EstablishmentBranchId branchId) =>
        _existingBranches.Add(branchId);

    public void SetWeeklyResponse(BranchWeeklyScheduleResponse response) =>
        _weeklyResponse = response;

    public void SetDayResponse(BranchDayScheduleResponse response) =>
        _dayResponse = response;

    public Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_existingBranches.Contains(branchId));
    }

    public Task<BranchWeeklyScheduleResponse?> GetWeeklyScheduleAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_weeklyResponse);
    }

    public Task<BranchDayScheduleResponse?> GetDayScheduleAsync(
        EstablishmentBranchId branchId,
        WeekDay dayOfWeek,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_dayResponse);
    }
}
