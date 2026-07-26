namespace Lyria.Domain.Establishments.Branches;

/// <summary>
/// Resultado del cálculo de disponibilidad de una sede.
/// </summary>
public sealed record BranchAvailabilityResult(
    BranchOpenStatus Status,
    ScheduleSource Source,
    DateOnly ScheduleDate,
    TimeOnly? OpensAtLocal,
    TimeOnly? ClosesAtLocal,
    string? Reason);

/// <summary>
/// Franja horaria genérica utilizada por el calculador de disponibilidad.
/// Representa tanto horarios semanales como especiales.
/// </summary>
public sealed record AvailabilityTimeSlot(
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool CrossesMidnight);

/// <summary>
/// Programación efectiva de un día para el cálculo de disponibilidad.
/// </summary>
public sealed record EffectiveSchedule(
    bool IsClosed,
    string? Reason,
    ScheduleSource Source,
    IReadOnlyList<AvailabilityTimeSlot> TimeSlots);

/// <summary>
/// Calculador puro de disponibilidad de una sede.
/// No depende de EF Core, HTTP ni infraestructura.
/// </summary>
/// <remarks>
/// Regla de prioridad: si existen horarios especiales activos para una fecha,
/// reemplazan completamente el horario semanal de esa fecha.
///
/// Regla de cruce de medianoche: una franja que comienza el día anterior
/// y cruza medianoche continúa siendo válida después de las 00:00.
/// Un cierre especial de la fecha actual no cancela automáticamente una
/// franja que comenzó la fecha anterior y todavía no terminó.
/// </remarks>
public static class BranchAvailabilityCalculator
{
    public static BranchAvailabilityResult Calculate(
        DateTime localDateTime,
        EffectiveSchedule? previousDaySchedule,
        EffectiveSchedule? currentDaySchedule)
    {
        var localTime = TimeOnly.FromDateTime(localDateTime);
        var localDate = DateOnly.FromDateTime(localDateTime);
        var previousDate = localDate.AddDays(-1);

        // 1. Check if we're inside a midnight-crossing slot from the previous day
        if (previousDaySchedule is not null && !previousDaySchedule.IsClosed)
        {
            foreach (var slot in previousDaySchedule.TimeSlots)
            {
                if (slot.CrossesMidnight && IsInMidnightCrossingSlotNextDay(localTime, slot))
                {
                    return new BranchAvailabilityResult(
                        BranchOpenStatus.Open,
                        previousDaySchedule.Source,
                        previousDate,
                        OpensAtLocal: null,
                        ClosesAtLocal: slot.ClosingTime,
                        previousDaySchedule.Reason);
                }
            }
        }

        // 2. No current-day schedule
        if (currentDaySchedule is null)
        {
            return new BranchAvailabilityResult(
                BranchOpenStatus.NoSchedule,
                ScheduleSource.None,
                localDate,
                OpensAtLocal: null,
                ClosesAtLocal: null,
                Reason: null);
        }

        // 3. Current day is marked as closed
        if (currentDaySchedule.IsClosed)
        {
            return new BranchAvailabilityResult(
                BranchOpenStatus.Closed,
                currentDaySchedule.Source,
                localDate,
                OpensAtLocal: null,
                ClosesAtLocal: null,
                currentDaySchedule.Reason);
        }

        // 4. No time slots on current day
        if (currentDaySchedule.TimeSlots.Count == 0)
        {
            return new BranchAvailabilityResult(
                BranchOpenStatus.NoSchedule,
                ScheduleSource.None,
                localDate,
                OpensAtLocal: null,
                ClosesAtLocal: null,
                Reason: null);
        }

        // 5. Check if currently inside a slot
        foreach (var slot in currentDaySchedule.TimeSlots)
        {
            if (IsInsideSlot(localTime, slot))
            {
                return new BranchAvailabilityResult(
                    BranchOpenStatus.Open,
                    currentDaySchedule.Source,
                    localDate,
                    OpensAtLocal: null,
                    ClosesAtLocal: slot.ClosingTime,
                    currentDaySchedule.Reason);
            }
        }

        // 6. Check if there's a later slot today
        var laterSlots = currentDaySchedule.TimeSlots
            .Where(s => s.OpeningTime > localTime)
            .OrderBy(s => s.OpeningTime)
            .ToList();

        if (laterSlots.Count > 0)
        {
            return new BranchAvailabilityResult(
                BranchOpenStatus.OpensLaterToday,
                currentDaySchedule.Source,
                localDate,
                OpensAtLocal: laterSlots[0].OpeningTime,
                ClosesAtLocal: null,
                currentDaySchedule.Reason);
        }

        // 7. All slots have passed
        return new BranchAvailabilityResult(
            BranchOpenStatus.Closed,
            currentDaySchedule.Source,
            localDate,
            OpensAtLocal: null,
            ClosesAtLocal: null,
            currentDaySchedule.Reason);
    }

    private static bool IsInsideSlot(TimeOnly localTime, AvailabilityTimeSlot slot)
    {
        if (slot.CrossesMidnight)
        {
            // e.g., 22:00-02:00 on the same day: open from 22:00 to midnight
            return localTime >= slot.OpeningTime;
        }

        // Normal slot: opening <= localTime < closing
        // A slot that starts exactly at localTime is considered open
        // A slot that ends exactly at localTime is considered closed
        return localTime >= slot.OpeningTime && localTime < slot.ClosingTime;
    }

    private static bool IsInMidnightCrossingSlotNextDay(TimeOnly localTime, AvailabilityTimeSlot slot)
    {
        // We're on the next day, checking the overflow portion of a midnight-crossing slot
        // e.g., slot is 22:00-02:00, we're checking if localTime < 02:00
        return localTime < slot.ClosingTime;
    }
}
