using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchSpecialScheduleReadService
{
    Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    Task<BranchSpecialSchedulesResponse?> GetByDateRangeAsync(
        EstablishmentBranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);
}
