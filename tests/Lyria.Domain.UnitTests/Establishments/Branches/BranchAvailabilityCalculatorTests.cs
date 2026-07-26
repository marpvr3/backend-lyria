using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class BranchAvailabilityCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 25);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);

    private static EffectiveSchedule OpenSchedule(
        ScheduleSource source = ScheduleSource.Weekly,
        string? reason = null,
        params AvailabilityTimeSlot[] slots)
    {
        return new EffectiveSchedule(false, reason, source, slots);
    }

    private static EffectiveSchedule ClosedSchedule(
        ScheduleSource source = ScheduleSource.Special,
        string? reason = null)
    {
        return new EffectiveSchedule(true, reason, source, []);
    }

    private static AvailabilityTimeSlot Slot(int openH, int openM, int closeH, int closeM, bool crossesMidnight = false)
    {
        return new AvailabilityTimeSlot(
            new TimeOnly(openH, openM),
            new TimeOnly(closeH, closeM),
            crossesMidnight);
    }

    private static DateTime LocalDateTime(int hour, int minute)
    {
        return Today.ToDateTime(new TimeOnly(hour, minute));
    }

    // --- No schedule at all ---

    [Fact]
    public void Calculate_NoPreviousNoCurrent_ReturnsNoSchedule()
    {
        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(10, 0), null, null);

        Assert.Equal(BranchOpenStatus.NoSchedule, result.Status);
        Assert.Equal(ScheduleSource.None, result.Source);
        Assert.Null(result.OpensAtLocal);
        Assert.Null(result.ClosesAtLocal);
    }

    // --- Current day is closed ---

    [Fact]
    public void Calculate_CurrentDayClosed_ReturnsClosed()
    {
        var current = ClosedSchedule(reason: "Festivo");

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(10, 0), null, current);

        Assert.Equal(BranchOpenStatus.Closed, result.Status);
        Assert.Equal(ScheduleSource.Special, result.Source);
        Assert.Equal("Festivo", result.Reason);
        Assert.Null(result.OpensAtLocal);
        Assert.Null(result.ClosesAtLocal);
    }

    // --- Current day has no time slots ---

    [Fact]
    public void Calculate_CurrentDayEmptySlots_ReturnsNoSchedule()
    {
        var current = OpenSchedule();

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(10, 0), null, current);

        Assert.Equal(BranchOpenStatus.NoSchedule, result.Status);
        Assert.Equal(ScheduleSource.None, result.Source);
    }

    // --- Currently inside a slot ---

    [Fact]
    public void Calculate_InsideSlot_ReturnsOpen()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(12, 0), null, current);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
        Assert.Equal(ScheduleSource.Weekly, result.Source);
        Assert.Null(result.OpensAtLocal);
        Assert.Equal(new TimeOnly(17, 0), result.ClosesAtLocal);
    }

    [Fact]
    public void Calculate_ExactlyAtOpeningTime_ReturnsOpen()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(9, 0), null, current);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
    }

    [Fact]
    public void Calculate_ExactlyAtClosingTime_ReturnsNotOpen()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(17, 0), null, current);

        // At closing time, the slot is considered closed
        Assert.NotEqual(BranchOpenStatus.Open, result.Status);
    }

    // --- Opens later today ---

    [Fact]
    public void Calculate_BeforeSlot_ReturnsOpensLaterToday()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(14, 0, 22, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(10, 0), null, current);

        Assert.Equal(BranchOpenStatus.OpensLaterToday, result.Status);
        Assert.Equal(new TimeOnly(14, 0), result.OpensAtLocal);
        Assert.Null(result.ClosesAtLocal);
    }

    [Fact]
    public void Calculate_MultipleSlots_ReturnsEarliestLaterSlot()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(14, 0, 16, 0),
            Slot(18, 0, 22, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(10, 0), null, current);

        Assert.Equal(BranchOpenStatus.OpensLaterToday, result.Status);
        Assert.Equal(new TimeOnly(14, 0), result.OpensAtLocal);
    }

    // --- All slots have passed ---

    [Fact]
    public void Calculate_AfterAllSlots_ReturnsClosed()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 12, 0),
            Slot(14, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(20, 0), null, current);

        Assert.Equal(BranchOpenStatus.Closed, result.Status);
    }

    // --- Midnight crossing: previous day ---

    [Fact]
    public void Calculate_InsideMidnightCrossingSlotFromPreviousDay_ReturnsOpen()
    {
        var previous = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(22, 0, 2, 0, crossesMidnight: true));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(1, 0), previous, null);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
        Assert.Equal(ScheduleSource.Weekly, result.Source);
        Assert.Equal(Yesterday, result.ScheduleDate);
        Assert.Null(result.OpensAtLocal);
        Assert.Equal(new TimeOnly(2, 0), result.ClosesAtLocal);
    }

    [Fact]
    public void Calculate_AfterMidnightCrossingSlotEnds_DoesNotReturnOpen()
    {
        var previous = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(22, 0, 2, 0, crossesMidnight: true));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(3, 0), previous, null);

        Assert.NotEqual(BranchOpenStatus.Open, result.Status);
    }

    // --- Midnight crossing: current day ---

    [Fact]
    public void Calculate_InsideMidnightCrossingSlotCurrentDay_ReturnsOpen()
    {
        var current = OpenSchedule(ScheduleSource.Special, null,
            Slot(22, 0, 3, 0, crossesMidnight: true));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(23, 0), null, current);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
        Assert.Equal(new TimeOnly(3, 0), result.ClosesAtLocal);
    }

    // --- Priority: previous day midnight slot vs current day closed ---

    [Fact]
    public void Calculate_PreviousDayMidnightSlot_TakesPriorityOverCurrentDayClosed()
    {
        var previous = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(22, 0, 4, 0, crossesMidnight: true));
        var current = ClosedSchedule(reason: "Cerrado por mantenimiento");

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(1, 0), previous, current);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
        Assert.Equal(Yesterday, result.ScheduleDate);
    }

    // --- Previous day closed: no midnight crossing ---

    [Fact]
    public void Calculate_PreviousDayClosed_NoMidnightEffect()
    {
        var previous = ClosedSchedule();
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(1, 0), previous, current);

        Assert.Equal(BranchOpenStatus.OpensLaterToday, result.Status);
        Assert.Equal(new TimeOnly(9, 0), result.OpensAtLocal);
    }

    // --- Special source ---

    [Fact]
    public void Calculate_SpecialScheduleSource_IsPreserved()
    {
        var current = OpenSchedule(ScheduleSource.Special, "Horario festivo",
            Slot(10, 0, 14, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(12, 0), null, current);

        Assert.Equal(BranchOpenStatus.Open, result.Status);
        Assert.Equal(ScheduleSource.Special, result.Source);
        Assert.Equal("Horario festivo", result.Reason);
    }

    // --- ScheduleDate ---

    [Fact]
    public void Calculate_CurrentDay_ScheduleDateIsToday()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 17, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(12, 0), null, current);

        Assert.Equal(Today, result.ScheduleDate);
    }

    // --- Between slots ---

    [Fact]
    public void Calculate_BetweenSlots_ReturnsOpensLaterToday()
    {
        var current = OpenSchedule(ScheduleSource.Weekly, null,
            Slot(9, 0, 12, 0),
            Slot(14, 0, 18, 0));

        var result = BranchAvailabilityCalculator.Calculate(
            LocalDateTime(13, 0), null, current);

        Assert.Equal(BranchOpenStatus.OpensLaterToday, result.Status);
        Assert.Equal(new TimeOnly(14, 0), result.OpensAtLocal);
    }
}
