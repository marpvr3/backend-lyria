using System.Globalization;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Construye instancias de <see cref="PublicBranchAvailabilityResponse"/>
/// a partir del resultado del cálculo de disponibilidad del dominio.
/// </summary>
public static class PublicBranchAvailabilityMapper
{
    public static PublicBranchAvailabilityResponse BuildResponse(
        BranchAvailabilityResult availability,
        DateTimeOffset evaluatedAtUtc,
        DateTime localDateTime,
        string timeZoneId,
        DateOnly localDate)
    {
        string? opensAtLocal = availability.OpensAtLocal is not null
            ? localDate.ToDateTime(availability.OpensAtLocal.Value)
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : null;

        string? closesAtLocal = availability.ClosesAtLocal is not null
            ? localDate.ToDateTime(availability.ClosesAtLocal.Value)
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : null;

        return new PublicBranchAvailabilityResponse(
            IsOpen: availability.Status == BranchOpenStatus.Open,
            Status: availability.Status.ToString(),
            StatusName: BranchOpenStatusNames.GetName(availability.Status),
            EvaluatedAtUtc: evaluatedAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            LocalDateTime: localDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            TimeZoneId: timeZoneId,
            ScheduleSource: availability.Source.ToString(),
            ScheduleDate: availability.ScheduleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            OpensAtLocal: opensAtLocal,
            ClosesAtLocal: closesAtLocal,
            Reason: availability.Reason);
    }
}
