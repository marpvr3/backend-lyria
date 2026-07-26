using System.Globalization;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Estado de disponibilidad compacto de una sede para vista pública.
/// </summary>
public sealed record PublicBranchAvailabilityResponse(
    bool IsOpen,
    string Status,
    string StatusName,
    string EvaluatedAtUtc,
    string LocalDateTime,
    string TimeZoneId,
    string ScheduleSource,
    string ScheduleDate,
    string? OpensAtLocal,
    string? ClosesAtLocal,
    string? Reason)
{
    /// <summary>
    /// Instancia por defecto que indica ausencia de programación.
    /// Utilizada como valor inicial por la capa de infraestructura cuando los handlers
    /// no han enriquecido aún la respuesta. Los handlers siempre lo reemplazan.
    /// </summary>
    public static readonly PublicBranchAvailabilityResponse Default = new(
        false, nameof(BranchOpenStatus.NoSchedule),
        BranchOpenStatusNames.GetName(BranchOpenStatus.NoSchedule),
        "", "", "UTC", "None", "", null, null, null);

    /// <summary>
    /// Crea una respuesta de disponibilidad indicando ausencia de programación.
    /// </summary>
    public static PublicBranchAvailabilityResponse CreateNoSchedule(DateTimeOffset evaluatedAtUtc)
    {
        string evaluatedStr = evaluatedAtUtc.UtcDateTime
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return new PublicBranchAvailabilityResponse(
            IsOpen: false,
            Status: BranchOpenStatus.NoSchedule.ToString(),
            StatusName: BranchOpenStatusNames.GetName(BranchOpenStatus.NoSchedule),
            EvaluatedAtUtc: evaluatedStr,
            LocalDateTime: evaluatedStr,
            TimeZoneId: "UTC",
            ScheduleSource: "None",
            ScheduleDate: DateOnly.FromDateTime(evaluatedAtUtc.UtcDateTime)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            OpensAtLocal: null,
            ClosesAtLocal: null,
            Reason: null);
    }
}

/// <summary>
/// Nombres centralizados en español para los estados de apertura.
/// </summary>
public static class BranchOpenStatusNames
{
    public static string GetName(BranchOpenStatus status) => status switch
    {
        BranchOpenStatus.Open => "Abierto",
        BranchOpenStatus.OpensLaterToday => "Abre más tarde",
        BranchOpenStatus.Closed => "Cerrado",
        BranchOpenStatus.NoSchedule => "Horario no disponible",
        _ => "Desconocido"
    };
}
