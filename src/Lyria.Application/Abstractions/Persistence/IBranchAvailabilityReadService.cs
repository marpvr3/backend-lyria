using Lyria.Application.Abstractions.Services;
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

    Task<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>
        GetAvailabilityContextsAsync(
            IReadOnlyCollection<EstablishmentBranchId> branchIds,
            DateTimeOffset evaluatedAtUtc,
            ITimeZoneService timeZoneService,
            CancellationToken cancellationToken);
}
