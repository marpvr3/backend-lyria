using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.BranchSchedules;

public static class BranchScheduleErrors
{
    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error OverlappingSlots(int dayOfWeek) =>
        Error.Conflict(
            "BranchSchedule.OverlappingSlots",
            $"Existen franjas horarias superpuestas para el día {dayOfWeek}.");

    public static Error ClosedDayConflict(int dayOfWeek) =>
        Error.Conflict(
            "BranchSchedule.ClosedDayConflict",
            $"El día {dayOfWeek} está marcado como cerrado y a la vez tiene franjas abiertas.");

    public static Error DuplicateClosedDay(int dayOfWeek) =>
        Error.Conflict(
            "BranchSchedule.DuplicateClosedDay",
            $"Solo puede existir una fila de cierre por día. El día {dayOfWeek} tiene más de una.");
}
