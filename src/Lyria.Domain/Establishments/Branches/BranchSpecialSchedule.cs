using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public sealed class BranchSpecialSchedule : Entity<BranchSpecialScheduleId>, IAuditableEntity
{
    public const int ReasonMaxLength = 500;

    public EstablishmentBranchId BranchId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly? OpeningTime { get; private set; }
    public TimeOnly? ClosingTime { get; private set; }
    public bool CrossesMidnight { get; private set; }
    public bool IsClosed { get; private set; }
    public string? Reason { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetCreatedAtUtc(DateTime value) => CreatedAtUtc = value;
    public void SetUpdatedAtUtc(DateTime value) => UpdatedAtUtc = value;

    private BranchSpecialSchedule() { }

    private BranchSpecialSchedule(
        BranchSpecialScheduleId id,
        EstablishmentBranchId branchId,
        DateOnly date,
        TimeOnly? openingTime,
        TimeOnly? closingTime,
        bool crossesMidnight,
        bool isClosed,
        string? reason)
        : base(id)
    {
        BranchId = branchId;
        Date = date;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        CrossesMidnight = crossesMidnight;
        IsClosed = isClosed;
        Reason = reason;
        IsActive = true;
    }

    public static BranchSpecialSchedule Create(
        BranchSpecialScheduleId id,
        EstablishmentBranchId branchId,
        DateOnly date,
        TimeOnly? openingTime,
        TimeOnly? closingTime,
        bool crossesMidnight,
        bool isClosed,
        string? reason)
    {
        ValidateBranchId(branchId);

        string? normalizedReason = NormalizeReason(reason);
        ValidateReason(normalizedReason);

        if (isClosed)
        {
            ValidateClosedDay(openingTime, closingTime, crossesMidnight);
        }
        else
        {
            ValidateOpenDay(openingTime, closingTime, crossesMidnight);
        }

        return new BranchSpecialSchedule(
            id, branchId, date, openingTime, closingTime, crossesMidnight, isClosed, normalizedReason);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        return reason.Trim();
    }

    public static void ValidateNoOverlaps(IReadOnlyList<BranchSpecialSchedule> schedules)
    {
        var openSlots = schedules.Where(s => !s.IsClosed).ToList();

        for (int i = 0; i < openSlots.Count; i++)
        {
            for (int j = i + 1; j < openSlots.Count; j++)
            {
                if (SlotsOverlap(openSlots[i], openSlots[j]))
                {
                    throw new EstablishmentBranchException(
                        $"Existen franjas horarias superpuestas para la fecha {openSlots[i].Date}.");
                }
            }
        }
    }

    public static void ValidateNoClosedDayConflicts(IReadOnlyList<BranchSpecialSchedule> schedules)
    {
        bool hasClosed = schedules.Any(s => s.IsClosed);
        bool hasOpen = schedules.Any(s => !s.IsClosed);

        if (hasClosed && hasOpen)
        {
            throw new EstablishmentBranchException(
                "Una fecha marcada como cerrada no puede tener franjas abiertas.");
        }

        int closedCount = schedules.Count(s => s.IsClosed);
        if (closedCount > 1)
        {
            throw new EstablishmentBranchException(
                $"Solo puede existir un registro cerrado por fecha. Se encontraron {closedCount}.");
        }
    }

    private static bool SlotsOverlap(BranchSpecialSchedule a, BranchSpecialSchedule b)
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

    private static void ValidateBranchId(EstablishmentBranchId branchId)
    {
        if (branchId.Value == Guid.Empty)
        {
            throw new EstablishmentBranchException("La sede del horario especial es obligatoria.");
        }
    }

    private static void ValidateReason(string? reason)
    {
        if (reason is not null && reason.Length > ReasonMaxLength)
        {
            throw new EstablishmentBranchException(
                $"El motivo no puede superar los {ReasonMaxLength} caracteres.");
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
