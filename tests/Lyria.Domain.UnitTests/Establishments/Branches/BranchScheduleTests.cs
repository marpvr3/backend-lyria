using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class BranchScheduleTests
{
    private static readonly EstablishmentBranchId BranchId = EstablishmentBranchId.New();

    private static BranchSchedule CreateOpenSlot(
        WeekDay day = WeekDay.Monday,
        TimeOnly? opening = null,
        TimeOnly? closing = null,
        bool crossesMidnight = false)
    {
        return BranchSchedule.Create(
            BranchScheduleId.New(),
            BranchId,
            day,
            opening ?? new TimeOnly(9, 0),
            closing ?? new TimeOnly(17, 0),
            crossesMidnight,
            isClosed: false);
    }

    private static BranchSchedule CreateClosedDay(WeekDay day = WeekDay.Sunday)
    {
        return BranchSchedule.Create(
            BranchScheduleId.New(),
            BranchId,
            day,
            openingTime: null,
            closingTime: null,
            crossesMidnight: false,
            isClosed: true);
    }

    [Fact]
    public void Create_NormalOpenSlot_SetsAllProperties()
    {
        var opening = new TimeOnly(12, 0);
        var closing = new TimeOnly(15, 0);

        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), BranchId, WeekDay.Monday,
            opening, closing, false, false);

        Assert.Equal(BranchId, schedule.BranchId);
        Assert.Equal(WeekDay.Monday, schedule.DayOfWeek);
        Assert.Equal(opening, schedule.OpeningTime);
        Assert.Equal(closing, schedule.ClosingTime);
        Assert.False(schedule.CrossesMidnight);
        Assert.False(schedule.IsClosed);
        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void Create_MidnightCrossingSlot_SetsProperties()
    {
        var opening = new TimeOnly(22, 0);
        var closing = new TimeOnly(2, 0);

        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), BranchId, WeekDay.Friday,
            opening, closing, true, false);

        Assert.Equal(opening, schedule.OpeningTime);
        Assert.Equal(closing, schedule.ClosingTime);
        Assert.True(schedule.CrossesMidnight);
        Assert.False(schedule.IsClosed);
    }

    [Fact]
    public void Create_ClosedDay_SetsProperties()
    {
        var schedule = CreateClosedDay(WeekDay.Sunday);

        Assert.Equal(WeekDay.Sunday, schedule.DayOfWeek);
        Assert.Null(schedule.OpeningTime);
        Assert.Null(schedule.ClosingTime);
        Assert.False(schedule.CrossesMidnight);
        Assert.True(schedule.IsClosed);
        Assert.True(schedule.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(255)]
    public void Create_InvalidDayOfWeek_Throws(int invalidDay)
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, (WeekDay)invalidDay,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false));
    }

    [Fact]
    public void Create_OpenSlot_NullOpeningTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Monday,
                null, new TimeOnly(17, 0), false, false));
    }

    [Fact]
    public void Create_OpenSlot_NullClosingTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Monday,
                new TimeOnly(9, 0), null, false, false));
    }

    [Fact]
    public void Create_OpenSlot_EqualTimes_Throws()
    {
        var time = new TimeOnly(12, 0);

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Monday,
                time, time, false, false));
    }

    [Fact]
    public void Create_ClosedDay_WithOpeningTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Sunday,
                new TimeOnly(9, 0), null, false, true));
    }

    [Fact]
    public void Create_ClosedDay_WithClosingTime_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Sunday,
                null, new TimeOnly(17, 0), false, true));
    }

    [Fact]
    public void Create_ClosedDay_WithCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Sunday,
                null, null, true, true));
    }

    [Fact]
    public void Create_ClosingBeforeOpening_WithoutCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Monday,
                new TimeOnly(22, 0), new TimeOnly(2, 0), false, false));
    }

    [Fact]
    public void Create_ClosingAfterOpening_WithCrossesMidnight_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.Create(
                BranchScheduleId.New(), BranchId, WeekDay.Monday,
                new TimeOnly(9, 0), new TimeOnly(17, 0), true, false));
    }

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

    [Fact]
    public void ValidateNoOverlaps_NonOverlapping_DoesNotThrow()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(12, 0), new TimeOnly(15, 0)),
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(19, 0), new TimeOnly(23, 30))
        };

        BranchSchedule.ValidateNoOverlaps(schedules);
    }

    [Fact]
    public void ValidateNoOverlaps_Overlapping_Throws()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(12, 0), new TimeOnly(16, 0)),
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(15, 0), new TimeOnly(20, 0))
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.ValidateNoOverlaps(schedules));
    }

    [Fact]
    public void ValidateNoOverlaps_MidnightCrossing_Overlapping_Throws()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateOpenSlot(WeekDay.Friday, new TimeOnly(22, 0), new TimeOnly(3, 0), crossesMidnight: true),
            CreateOpenSlot(WeekDay.Friday, new TimeOnly(1, 0), new TimeOnly(5, 0))
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.ValidateNoOverlaps(schedules));
    }

    [Fact]
    public void ValidateNoOverlaps_DifferentDays_DoesNotThrow()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            CreateOpenSlot(WeekDay.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0))
        };

        BranchSchedule.ValidateNoOverlaps(schedules);
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_ClosedDayWithOpenSlot_Throws()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateClosedDay(WeekDay.Sunday),
            CreateOpenSlot(WeekDay.Sunday, new TimeOnly(10, 0), new TimeOnly(14, 0))
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.ValidateNoClosedDayConflicts(schedules));
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_DuplicateClosedDay_Throws()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateClosedDay(WeekDay.Sunday),
            CreateClosedDay(WeekDay.Sunday)
        };

        Assert.Throws<EstablishmentBranchException>(() =>
            BranchSchedule.ValidateNoClosedDayConflicts(schedules));
    }

    [Fact]
    public void ValidateNoClosedDayConflicts_ValidMix_DoesNotThrow()
    {
        var schedules = new List<BranchSchedule>
        {
            CreateOpenSlot(WeekDay.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            CreateClosedDay(WeekDay.Sunday)
        };

        BranchSchedule.ValidateNoClosedDayConflicts(schedules);
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(BranchSchedule).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }

    [Fact]
    public void ImplementsIAuditableEntity()
    {
        Assert.True(typeof(IAuditableEntity).IsAssignableFrom(typeof(BranchSchedule)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Create_ValidDayOfWeek_DoesNotThrow(int day)
    {
        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), BranchId, (WeekDay)day,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false);

        Assert.Equal((WeekDay)day, schedule.DayOfWeek);
    }
}
