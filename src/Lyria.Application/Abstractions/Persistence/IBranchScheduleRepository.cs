using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchScheduleRepository
{
    Task<List<BranchSchedule>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    Task ReplaceSchedulesAsync(
        EstablishmentBranchId branchId,
        IReadOnlyList<BranchSchedule> newSchedules,
        CancellationToken cancellationToken);
}
