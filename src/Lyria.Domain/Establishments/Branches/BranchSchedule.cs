using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public sealed class BranchSchedule : Entity<BranchScheduleId>, IAuditableEntity
{
    public EstablishmentBranchId BranchId { get; private set; }
    public WeekDay DayOfWeek { get; private set; }
    public TimeOnly? OpeningTime { get; private set; }
    public TimeOnly? ClosingTime { get; private set; }
    public bool CrossesMidnight { get; private set; }
    public bool IsClosed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private BranchSchedule() { }

    private BranchSchedule(
        BranchScheduleId id,
        EstablishmentBranchId branchId,
        WeekDay dayOfWeek,
        TimeOnly? openingTime,
        TimeOnly? closingTime,
        bool crossesMidnight,
        bool isClosed)
        : base(id)
    {
        BranchId = branchId;
        DayOfWeek = dayOfWeek;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        CrossesMidnight = crossesMidnight;
        IsClosed = isClosed;
        IsActive = true;
    }

    public static BranchSchedule Create(
        BranchScheduleId id,
        EstablishmentBranchId branchId,
        WeekDay dayOfWeek,
        TimeOnly? openingTime,
        TimeOnly? closingTime,
        bool crossesMidnight,
        bool isClosed)
    {
        ValidateDayOfWeek(dayOfWeek);

        if (isClosed)
        {
            ValidateClosedDay(openingTime, closingTime, crossesMidnight);
        }
        else
        {
            ValidateOpenDay(openingTime, closingTime, crossesMidnight);
        }

        return new BranchSchedule(id, branchId, dayOfWeek, openingTime, closingTime, crossesMidnight, isClosed);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static void ValidateNoOverlaps(IReadOnlyList<BranchSchedule> schedules)
    {
        var openSlots = schedules.Where(s => !s.IsClosed).ToList();

        var grouped = openSlots.GroupBy(s => s.DayOfWeek);

        foreach (var group in grouped)
        {
            var slots = group.ToList();

            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (SlotsOverlap(slots[i], slots[j]))
                    {
                        throw new EstablishmentBranchException(
                            $"Existen franjas horarias superpuestas para el día {(int)slots[i].DayOfWeek}.");
                    }
                }
            }
        }
    }

    public static void ValidateNoClosedDayConflicts(IReadOnlyList<BranchSchedule> schedules)
    {
        var grouped = schedules.GroupBy(s => s.DayOfWeek);

        foreach (var group in grouped)
        {
            var slots = group.ToList();
            bool hasClosed = slots.Any(s => s.IsClosed);
            bool hasOpen = slots.Any(s => !s.IsClosed);

            if (hasClosed && hasOpen)
            {
                throw new EstablishmentBranchException(
                    $"El día {(int)group.Key} está marcado como cerrado y a la vez tiene franjas abiertas.");
            }

            int closedCount = slots.Count(s => s.IsClosed);
            if (closedCount > 1)
            {
                throw new EstablishmentBranchException(
                    $"Solo puede existir una fila de cierre por día. El día {(int)group.Key} tiene {closedCount}.");
            }
        }
    }

    private static bool SlotsOverlap(BranchSchedule a, BranchSchedule b)
    {
        int aStart = a.OpeningTime!.Value.Hour * 60 + a.OpeningTime.Value.Minute;
        int aEnd = a.ClosingTime!.Value.Hour * 60 + a.ClosingTime.Value.Minute;
        int bStart = b.OpeningTime!.Value.Hour * 60 + b.OpeningTime.Value.Minute;
        int bEnd = b.ClosingTime!.Value.Hour * 60 + b.ClosingTime.Value.Minute;

        if (a.CrossesMidnight)
        {
            aEnd += 24 * 60;
        }

        if (b.CrossesMidnight)
        {
            bEnd += 24 * 60;
        }

        // For midnight-crossing slots, we also need to check the wrapped portion
        if (a.CrossesMidnight || b.CrossesMidnight)
        {
            return IntervalsOverlap(aStart, aEnd, bStart, bEnd)
                || IntervalsOverlap(aStart, aEnd, bStart + 24 * 60, bEnd + 24 * 60)
                || IntervalsOverlap(aStart + 24 * 60, aEnd + 24 * 60, bStart, bEnd);
        }

        return IntervalsOverlap(aStart, aEnd, bStart, bEnd);
    }

    private static bool IntervalsOverlap(int aStart, int aEnd, int bStart, int bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }

    private static void ValidateDayOfWeek(WeekDay dayOfWeek)
    {
        int day = (int)dayOfWeek;
        if (day < 1 || day > 7)
        {
            throw new EstablishmentBranchException(
                "El día de la semana debe estar entre 1 (lunes) y 7 (domingo).");
        }
    }

    private static void ValidateClosedDay(TimeOnly? openingTime, TimeOnly? closingTime, bool crossesMidnight)
    {
        if (openingTime is not null)
        {
            throw new EstablishmentBranchException(
                "Un día cerrado no puede tener hora de apertura.");
        }

        if (closingTime is not null)
        {
            throw new EstablishmentBranchException(
                "Un día cerrado no puede tener hora de cierre.");
        }

        if (crossesMidnight)
        {
            throw new EstablishmentBranchException(
                "Un día cerrado no puede marcar cruce de medianoche.");
        }
    }

    private static void ValidateOpenDay(TimeOnly? openingTime, TimeOnly? closingTime, bool crossesMidnight)
    {
        if (openingTime is null)
        {
            throw new EstablishmentBranchException(
                "La hora de apertura es obligatoria para una franja abierta.");
        }

        if (closingTime is null)
        {
            throw new EstablishmentBranchException(
                "La hora de cierre es obligatoria para una franja abierta.");
        }

        if (openingTime.Value == closingTime.Value)
        {
            throw new EstablishmentBranchException(
                "La hora de apertura y la hora de cierre no pueden ser iguales.");
        }

        if (closingTime.Value < openingTime.Value && !crossesMidnight)
        {
            throw new EstablishmentBranchException(
                "Si la hora de cierre es anterior a la hora de apertura, se debe marcar cruce de medianoche.");
        }

        if (closingTime.Value > openingTime.Value && crossesMidnight)
        {
            throw new EstablishmentBranchException(
                "Si la hora de cierre es posterior a la hora de apertura, no se debe marcar cruce de medianoche.");
        }
    }
}
