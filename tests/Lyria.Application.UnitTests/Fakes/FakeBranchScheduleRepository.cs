using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchScheduleRepository : IBranchScheduleRepository
{
    private readonly List<BranchSchedule> _schedules = [];
    public IReadOnlyList<BranchSchedule> Added => _schedules;
    public int ReplaceCallCount { get; private set; }

    public void Seed(BranchSchedule schedule) => _schedules.Add(schedule);

    public Task<List<BranchSchedule>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        var result = _schedules
            .Where(s => s.BranchId == branchId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .ToList();
        return Task.FromResult(result);
    }

    public Task ReplaceSchedulesAsync(
        EstablishmentBranchId branchId,
        IReadOnlyList<BranchSchedule> newSchedules,
        CancellationToken cancellationToken)
    {
        ReplaceCallCount++;

        var existing = _schedules.Where(s => s.BranchId == branchId && s.IsActive).ToList();
        foreach (var schedule in existing)
        {
            schedule.Deactivate();
        }

        _schedules.AddRange(newSchedules);
        return Task.CompletedTask;
    }
}
