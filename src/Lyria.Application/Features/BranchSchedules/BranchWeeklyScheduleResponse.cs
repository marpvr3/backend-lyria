namespace Lyria.Application.Features.BranchSchedules;

/// <summary>
/// Programación semanal completa de una sede.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="Schedules">Días de la semana con sus franjas horarias.</param>
public sealed record BranchWeeklyScheduleResponse(
    Guid BranchId,
    IReadOnlyList<BranchDayScheduleResponse> Schedules);

/// <summary>
/// Programación de un día específico de la semana.
/// </summary>
/// <param name="DayOfWeek">Número del día (1=Lunes, 7=Domingo).</param>
/// <param name="DayName">Nombre del día en español.</param>
/// <param name="IsClosed">Indica si la sede está cerrada ese día.</param>
/// <param name="TimeSlots">Franjas horarias del día.</param>
public sealed record BranchDayScheduleResponse(
    int DayOfWeek,
    string DayName,
    bool IsClosed,
    IReadOnlyList<BranchTimeSlotResponse> TimeSlots);

/// <summary>
/// Franja horaria de una sede.
/// </summary>
/// <param name="Id">Identificador de la franja.</param>
/// <param name="OpeningTime">Hora de apertura (HH:mm).</param>
/// <param name="ClosingTime">Hora de cierre (HH:mm).</param>
/// <param name="CrossesMidnight">Indica si cruza medianoche.</param>
public sealed record BranchTimeSlotResponse(
    Guid Id,
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight);
