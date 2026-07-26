using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchSpecialScheduleRepository
{
    Task<List<BranchSpecialSchedule>> GetActiveByBranchAndDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        CancellationToken cancellationToken);

    Task ReplaceForDateAsync(
        EstablishmentBranchId branchId,
        DateOnly scheduleDate,
        IReadOnlyList<BranchSpecialSchedule> newSchedules,
        CancellationToken cancellationToken);
}
