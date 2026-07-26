using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public sealed record BranchAvailabilityContext(
    Guid BranchId,
    string TimeZoneId,
    bool IsActive,
    EffectiveSchedule? PreviousDaySchedule,
    EffectiveSchedule? CurrentDaySchedule);

public interface IBranchAvailabilityReadService
{
    Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
        EstablishmentBranchId branchId,
        DateOnly localDate,
        CancellationToken cancellationToken);
}
