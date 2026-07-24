using Lyria.Application.Features.BranchSchedules;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchScheduleReadService
{
    Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    Task<BranchWeeklyScheduleResponse?> GetWeeklyScheduleAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    Task<BranchDayScheduleResponse?> GetDayScheduleAsync(
        EstablishmentBranchId branchId,
        WeekDay dayOfWeek,
        CancellationToken cancellationToken);
}
