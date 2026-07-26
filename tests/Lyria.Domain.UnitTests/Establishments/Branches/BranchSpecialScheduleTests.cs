using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class BranchSpecialScheduleTests
{
    private static readonly EstablishmentBranchId BranchId = EstablishmentBranchId.New();
    private static readonly DateOnly TestDate = new(2026, 12, 25);

    private static BranchSpecialSchedule CreateOpenSlot(
        TimeOnly? opening = null,
        TimeOnly? closing = null,
        bool crossesMidnight = false,
        string? reason = null,
        DateOnly? date = null)
    {
        return BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(),
            BranchId,
            date ?? TestDate,
            opening ?? new TimeOnly(9, 0),
            closing ?? new TimeOnly(17, 0),
            crossesMidnight,
            isClosed: false,
            reason);
    }

    private static BranchSpecialSchedule CreateClosedDay(
        string? reason = null,
        DateOnly? date = null)
    {
        return BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(),
            BranchId,
            date ?? TestDate,
            openingTime: null,
            closingTime: null,
            crossesMidnight: false,
            isClosed: true,
            reason);
    }

    // --- Create: open slot ---

    [Fact]
    public void Create_NormalOpenSlot_SetsAllProperties()
    {
        var opening = new TimeOnly(12, 0);
        var closing = new TimeOnly(15, 0);

        var schedule = BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(), BranchId, TestDate,
            opening, closing, false, false, "Horario festivo");

        Assert.Equal(BranchId, schedule.BranchId);
        Assert.Equal(TestDate, schedule.Date);
        Assert.Equal(opening, schedule.OpeningTime);
        Assert.Equal(closing, schedule.ClosingTime);
        Assert.False(schedule.CrossesMidnight);
        Assert.False(schedule.IsClosed);
        Assert.Equal("Horario festivo", schedule.Reason);
        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void Create_MidnightCrossingSlot_SetsProperties()
    {
        var opening = new TimeOnly(22, 0);
        var closing = new TimeOnly(2, 0);

        var schedule = BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(), BranchId, TestDate,
            opening, closing, true, false, null);

        Assert.Equal(opening, schedule.OpeningTime);
        Assert.Equal(closing, schedule.ClosingTime);
        Assert.True(schedule.CrossesMidnight);
        Assert.False(schedule.IsClosed);
    }

    // --- Create: closed day ---

    [Fact]
    public void Create_ClosedDay_SetsProperties()
    {
        var schedule = CreateClosedDay(reason: "Festivo");

        Assert.Equal(TestDate, schedule.Date);
        Assert.Null(schedule.OpeningTime);
        Assert.Null(schedule.ClosingTime);
        Assert.False(schedule.CrossesMidnight);
        Assert.True(schedule.IsClosed);
        Assert.Equal("Festivo", schedule.Reason);
        Assert.True(schedule.IsActive);
    }

    // --- BranchId validation ---

    [Fact]
    public void Create_EmptyBranchId_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(),
                new EstablishmentBranchId(Guid.Empty),
                TestDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0),
                false, false, null));
    }

    // --- Reason validation ---

    [Fact]
    public void Create_ReasonExceedsMaxLength_Throws()
    {
        var longReason = new string('A', BranchSpecialSchedule.ReasonMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0),
                false, false, longReason));
    }

    [Fact]
    public void Create_ReasonAtMaxLength_DoesNotThrow()
    {
        var reason = new string('A', BranchSpecialSchedule.ReasonMaxLength);

        var schedule = BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(), BranchId, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            false, false, reason);

        Assert.Equal(reason, schedule.Reason);
    }

    // --- Closed day validation ---

    [Fact]
    public void Create_ClosedDay_WithOpeningTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                new TimeOnly(9, 0), null, false, true, null));
    }

    [Fact]
    public void Create_ClosedDay_WithClosingTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                null, new TimeOnly(17, 0), false, true, null));
    }

    [Fact]
    public void Create_ClosedDay_WithCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                null, null, true, true, null));
    }

    // --- Open day validation ---

    [Fact]
    public void Create_OpenSlot_NullOpeningTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                null, new TimeOnly(17, 0), false, false, null));
    }

    [Fact]
    public void Create_OpenSlot_NullClosingTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                new TimeOnly(9, 0), null, false, false, null));
    }

    [Fact]
    public void Create_OpenSlot_EqualTimes_Throws()
    {
        var time = new TimeOnly(12, 0);

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                time, time, false, false, null));
    }

    [Fact]
    public void Create_ClosingBeforeOpening_WithoutCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                new TimeOnly(22, 0), new TimeOnly(2, 0), false, false, null));
    }

    [Fact]
    public void Create_ClosingAfterOpening_WithCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.Create(
                BranchSpecialScheduleId.New(), BranchId, TestDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0), true, false, null));
    }

    // --- Activate / Deactivate ---

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var schedule = CreateOpenSlot();
        schedule.Deactivate();

        schedule.Activate();

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var schedule = CreateOpenSlot();

        schedule.Deactivate();

        Assert.False(schedule.IsActive);
    }

    // --- Reason normalization ---

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Festivo  ", "Festivo")]
    [InlineData("Navidad", "Navidad")]
    public void NormalizeReason_ReturnsExpected(string? input, string? expected)
    {
        Assert.Equal(expected, BranchSpecialSchedule.NormalizeReason(input));
    }

    [Fact]
    public void Create_TrimsReason()
    {
        var schedule = BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(), BranchId, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            false, false, "  Navidad  ");

        Assert.Equal("Navidad", schedule.Reason);
    }

    [Fact]
    public void Create_WhitespaceReason_BecomesNull()
    {
        var schedule = BranchSpecialSchedule.Create(
            BranchSpecialScheduleId.New(), BranchId, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            false, false, "   ");

        Assert.Null(schedule.Reason);
    }

    // --- ValidateNoOverlaps ---

    [Fact]
    public void ValidateNoOverlaps_NonOverlapping_DoesNotThrow()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateOpenSlot(new TimeOnly(12, 0), new TimeOnly(15, 0)),
            CreateOpenSlot(new TimeOnly(19, 0), new TimeOnly(23, 30))
        };

        BranchSpecialSchedule.ValidateNoOverlaps(schedules);
    }

    [Fact]
    public void ValidateNoOverlaps_Overlapping_Throws()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateOpenSlot(new TimeOnly(12, 0), new TimeOnly(16, 0)),
            CreateOpenSlot(new TimeOnly(15, 0), new TimeOnly(20, 0))
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.ValidateNoOverlaps(schedules));
    }

    [Fact]
    public void ValidateNoOverlaps_MidnightCrossing_Overlapping_Throws()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateOpenSlot(new TimeOnly(22, 0), new TimeOnly(3, 0), crossesMidnight: true),
            CreateOpenSlot(new TimeOnly(1, 0), new TimeOnly(5, 0))
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.ValidateNoOverlaps(schedules));
    }

    [Fact]
    public void ValidateNoOverlaps_ClosedDaysIgnored_DoesNotThrow()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateClosedDay(),
            CreateOpenSlot(new TimeOnly(9, 0), new TimeOnly(12, 0))
        };

        // Closed days are filtered out so no overlap check applies
        BranchSpecialSchedule.ValidateNoOverlaps(schedules);
    }

    // --- ValidateNoClosedDayConflicts ---

    [Fact]
    public void ValidateNoClosedDayConflicts_ClosedDayWithOpenSlot_Throws()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateClosedDay(),
            CreateOpenSlot()
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.ValidateNoClosedDayConflicts(schedules));
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_DuplicateClosedDay_Throws()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateClosedDay(),
            CreateClosedDay()
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSpecialSchedule.ValidateNoClosedDayConflicts(schedules));
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_OnlyOpenSlots_DoesNotThrow()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateOpenSlot(new TimeOnly(9, 0), new TimeOnly(12, 0)),
            CreateOpenSlot(new TimeOnly(14, 0), new TimeOnly(18, 0))
        };

        BranchSpecialSchedule.ValidateNoClosedDayConflicts(schedules);
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_SingleClosedDay_DoesNotThrow()
    {
        var schedules = new List<BranchSpecialSchedule>
        {
            CreateClosedDay()
        };

        BranchSpecialSchedule.ValidateNoClosedDayConflicts(schedules);
    }

    // --- Encapsulation ---

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(BranchSpecialSchedule).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }

    [Fact]
    public void ImplementsIAuditableEntity()
    {
        Assert.True(typeof(IAuditableEntity).IsAssignableFrom(typeof(BranchSpecialSchedule)));
    }
}
