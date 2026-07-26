using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.BranchSpecialSchedules;

public static class BranchSpecialScheduleErrors
{
    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error BranchInactive(Guid branchId) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error InvalidTimeZone(string timeZoneId) =>
        Error.Validation(
            "BranchSpecialSchedule.InvalidTimeZone",
            $"La zona horaria '{timeZoneId}' no es válida.");

    public static readonly Error InvalidDateRange =
        Error.Validation(
            "BranchSpecialSchedule.InvalidDateRange",
            "El rango de fechas no es válido.");

    public static Error DateRangeExceedsMaximum(int maxDays) =>
        Error.Validation(
            "BranchSpecialSchedule.DateRangeExceedsMaximum",
            $"El rango de fechas no puede superar los {maxDays} días.");

    public static Error OverlappingSlots(DateOnly date) =>
        Error.Conflict(
            "BranchSpecialSchedule.OverlappingSlots",
            $"Existen franjas horarias superpuestas para la fecha {date}.");

    public static Error ClosedDayConflict(DateOnly date) =>
        Error.Conflict(
            "BranchSpecialSchedule.ClosedDayConflict",
            $"La fecha {date} está marcada como cerrada y a la vez tiene franjas abiertas.");

    public static Error DuplicateClosedDay(DateOnly date) =>
        Error.Conflict(
            "BranchSpecialSchedule.DuplicateClosedDay",
            $"Solo puede existir una fila de cierre por fecha. La fecha {date} tiene más de una.");

    public static readonly Error FromDateAfterToDate =
        Error.Validation(
            "BranchSpecialSchedule.FromDateAfterToDate",
            "La fecha de inicio no puede ser posterior a la fecha de fin.");
}
