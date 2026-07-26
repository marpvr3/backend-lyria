using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchSpecialScheduleRepository : IBranchSpecialScheduleRepository
{
    private readonly List<BranchSpecialSchedule> _schedules = [];
    public int ReplaceCallCount { get; private set; }

    public void Seed(BranchSpecialSchedule schedule) => _schedules.Add(schedule);

    public Task<List<BranchSpecialSchedule>> GetActiveByBranchAndDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        CancellationToken cancellationToken)
    {
        var result = _schedules
            .Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive)
            .ToList();
        return Task.FromResult(result);
    }

    public Task ReplaceForDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        IReadOnlyList<BranchSpecialSchedule> newSchedules,
        CancellationToken cancellationToken)
    {
        ReplaceCallCount++;

        var existing = _schedules
            .Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive)
            .ToList();

        foreach (var schedule in existing)
        {
            schedule.Deactivate();
        }

        _schedules.AddRange(newSchedules);
        return Task.CompletedTask;
    }
}
