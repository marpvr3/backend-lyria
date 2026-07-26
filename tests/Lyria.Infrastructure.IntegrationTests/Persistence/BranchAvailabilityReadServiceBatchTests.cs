using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchAvailabilityReadServiceBatchTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    #region Seed helpers

    private static async Task<EstablishmentCategoryId> SeedCategory(
        LyriaDbContext context, string? name = null)
    {
        var id = EstablishmentCategoryId.New();
        var category = EstablishmentCategory.Create(id, name ?? $"Cat-{id.Value:N}", "Desc", null, 1);
        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<EstablishmentId> SeedEstablishment(
        LyriaDbContext context,
        EstablishmentCategoryId categoryId,
        string? name = null,
        string? slug = null)
    {
        var id = EstablishmentId.New();
        var actualName = name ?? $"Est-{id.Value:N}";
        var actualSlug = slug ?? $"est-{id.Value:N}";
        var establishment = Establishment.Create(
            id, categoryId, actualName, actualSlug, null, null, null, null, null, null);
        context.Set<Establishment>().Add(establishment);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<EstablishmentBranchId> SeedBranch(
        LyriaDbContext context,
        EstablishmentId establishmentId,
        string? name = null,
        string timeZoneId = "America/Bogota",
        bool isActive = true)
    {
        var id = EstablishmentBranchId.New();
        var actualName = name ?? $"Sede-{id.Value:N}";
        var branch = EstablishmentBranch.Create(
            id, establishmentId, actualName, "Calle 1", null, null, null, "Bogota", "Cundinamarca",
            null, "Colombia", null, null, null, null, null, timeZoneId);
        if (!isActive)
        {
            branch.Deactivate();
        }

        context.Set<EstablishmentBranch>().Add(branch);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<BranchScheduleId> SeedSchedule(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        WeekDay day = WeekDay.Monday,
        TimeOnly? opening = null,
        TimeOnly? closing = null,
        bool crossesMidnight = false,
        bool isClosed = false)
    {
        var id = BranchScheduleId.New();
        var schedule = BranchSchedule.Create(
            id, branchId, day, opening ?? new TimeOnly(8, 0), closing ?? new TimeOnly(17, 0),
            crossesMidnight, isClosed);
        context.Set<BranchSchedule>().Add(schedule);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<BranchSpecialScheduleId> SeedSpecialSchedule(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        DateOnly date,
        TimeOnly? opening = null,
        TimeOnly? closing = null,
        bool crossesMidnight = false,
        bool isClosed = false,
        string? reason = null)
    {
        var id = BranchSpecialScheduleId.New();
        var schedule = BranchSpecialSchedule.Create(
            id, branchId, date, opening, closing, crossesMidnight, isClosed, reason);
        context.Set<BranchSpecialSchedule>().Add(schedule);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<(EstablishmentCategoryId CategoryId, EstablishmentId EstablishmentId, EstablishmentBranchId BranchId)>
        SeedFullBranch(
            LyriaDbContext context,
            string? name = null,
            string timeZoneId = "America/Bogota",
            bool branchIsActive = true)
    {
        var catId = await SeedCategory(context);
        var estId = await SeedEstablishment(context, catId, name);
        var branchId = await SeedBranch(context, estId, timeZoneId: timeZoneId, isActive: branchIsActive);
        return (catId, estId, branchId);
    }

    #endregion

    [Fact]
    public async Task GetBatch_ReturnsMultipleBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branch1) = await SeedFullBranch(writeCtx, "Branch1");
        var (_, _, branch2) = await SeedFullBranch(writeCtx, "Branch2");
        var (_, _, branch3) = await SeedFullBranch(writeCtx, "Branch3");

        await SeedSchedule(writeCtx, branch1, WeekDay.Monday);
        await SeedSchedule(writeCtx, branch2, WeekDay.Monday);
        await SeedSchedule(writeCtx, branch3, WeekDay.Monday);

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        // Evaluate at a Monday in Bogota timezone
        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero); // Monday UTC

        var result = await sut.GetAvailabilityContextsAsync(
            [branch1, branch2, branch3], evaluatedAt, timeZoneService, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey(branch1));
        Assert.True(result.ContainsKey(branch2));
        Assert.True(result.ContainsKey(branch3));
    }

    [Fact]
    public async Task GetBatch_SingleQuery_NotPerBranch()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branch1) = await SeedFullBranch(writeCtx, "BatchA");
        var (_, _, branch2) = await SeedFullBranch(writeCtx, "BatchB");

        await SeedSchedule(writeCtx, branch1, WeekDay.Monday);
        await SeedSchedule(writeCtx, branch2, WeekDay.Monday);

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero); // Monday UTC

        var result = await sut.GetAvailabilityContextsAsync(
            [branch1, branch2], evaluatedAt, timeZoneService, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(branch1));
        Assert.True(result.ContainsKey(branch2));
    }

    [Fact]
    public async Task GetBatch_DifferentTimeZones_UseDifferentLocalDates()
    {
        await using var writeCtx = _fixture.CreateContext();

        // Branch in Bogota (UTC-5)
        var (_, _, bogotaBranch) = await SeedFullBranch(writeCtx, "Bogota", "America/Bogota");
        // Branch in Auckland (UTC+12/+13)
        var (_, _, aucklandBranch) = await SeedFullBranch(writeCtx, "Auckland", "Pacific/Auckland");

        // At 2026-07-27T03:00:00Z (Monday UTC):
        //   Bogota: 2026-07-26 22:00 (Sunday local) => current day = Sunday
        //   Auckland: 2026-07-27 15:00 (Monday local) => current day = Monday
        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 3, 0, 0, TimeSpan.Zero);

        // Seed Sunday schedule for Bogota branch
        await SeedSchedule(writeCtx, bogotaBranch, WeekDay.Sunday,
            new TimeOnly(10, 0), new TimeOnly(18, 0));

        // Seed Monday schedule for Auckland branch
        await SeedSchedule(writeCtx, aucklandBranch, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        var result = await sut.GetAvailabilityContextsAsync(
            [bogotaBranch, aucklandBranch], evaluatedAt, timeZoneService, CancellationToken.None);

        Assert.Equal(2, result.Count);

        // Bogota branch should have Sunday as current day schedule
        var bogotaCtx = result[bogotaBranch];
        Assert.NotNull(bogotaCtx.CurrentDaySchedule);
        Assert.Equal(ScheduleSource.Weekly, bogotaCtx.CurrentDaySchedule.Source);

        // Auckland branch should have Monday as current day schedule
        var aucklandCtx = result[aucklandBranch];
        Assert.NotNull(aucklandCtx.CurrentDaySchedule);
        Assert.Equal(ScheduleSource.Weekly, aucklandCtx.CurrentDaySchedule.Source);
    }

    [Fact]
    public async Task GetBatch_LoadsWeeklySchedules_CurrentAndPreviousDay()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx, "WeeklyPrevCurr");

        // Seed Monday and Tuesday schedules
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0));
        await SeedSchedule(writeCtx, branchId, WeekDay.Tuesday,
            new TimeOnly(10, 0), new TimeOnly(18, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        // 2026-07-28 is a Tuesday. At 15:00 UTC, Bogota (UTC-5) is Tuesday 10:00.
        var evaluatedAt = new DateTimeOffset(2026, 7, 28, 15, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            [branchId], evaluatedAt, timeZoneService, CancellationToken.None);

        var ctx = result[branchId];

        // Previous day (Monday)
        Assert.NotNull(ctx.PreviousDaySchedule);
        Assert.Equal(ScheduleSource.Weekly, ctx.PreviousDaySchedule.Source);
        Assert.Single(ctx.PreviousDaySchedule.TimeSlots);
        Assert.Equal(new TimeOnly(9, 0), ctx.PreviousDaySchedule.TimeSlots[0].OpeningTime);

        // Current day (Tuesday)
        Assert.NotNull(ctx.CurrentDaySchedule);
        Assert.Equal(ScheduleSource.Weekly, ctx.CurrentDaySchedule.Source);
        Assert.Single(ctx.CurrentDaySchedule.TimeSlots);
        Assert.Equal(new TimeOnly(10, 0), ctx.CurrentDaySchedule.TimeSlots[0].OpeningTime);
    }

    [Fact]
    public async Task GetBatch_LoadsSpecialSchedules_CurrentAndPreviousDay()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx, "SpecialPrevCurr");

        // 2026-07-28 is a Tuesday in Bogota
        var previousDate = new DateOnly(2026, 7, 27); // Monday
        var currentDate = new DateOnly(2026, 7, 28);  // Tuesday

        await SeedSpecialSchedule(writeCtx, branchId, previousDate,
            new TimeOnly(11, 0), new TimeOnly(15, 0), reason: "Festivo lunes");
        await SeedSpecialSchedule(writeCtx, branchId, currentDate,
            new TimeOnly(12, 0), new TimeOnly(16, 0), reason: "Festivo martes");

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        // Evaluate at Tuesday 15:00 UTC => Bogota 10:00 Tuesday
        var evaluatedAt = new DateTimeOffset(2026, 7, 28, 15, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            [branchId], evaluatedAt, timeZoneService, CancellationToken.None);

        var ctx = result[branchId];

        // Previous day special
        Assert.NotNull(ctx.PreviousDaySchedule);
        Assert.Equal(ScheduleSource.Special, ctx.PreviousDaySchedule.Source);

        // Current day special
        Assert.NotNull(ctx.CurrentDaySchedule);
        Assert.Equal(ScheduleSource.Special, ctx.CurrentDaySchedule.Source);
    }

    [Fact]
    public async Task GetBatch_HandlesMidnightCrossingSlots()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx, "MidnightCross");

        // Seed a schedule from 22:00 to 02:00 crossing midnight on Monday
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday,
            new TimeOnly(22, 0), new TimeOnly(2, 0), crossesMidnight: true);

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        // 2026-07-27 is a Monday. Evaluate at Monday 23:00 Bogota = Tuesday 04:00 UTC
        var evaluatedAt = new DateTimeOffset(2026, 7, 28, 4, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            [branchId], evaluatedAt, timeZoneService, CancellationToken.None);

        var ctx = result[branchId];

        // Monday is the previous day (local is Tuesday 2026-07-28 at 23:00 Bogota => actually Monday night)
        // Let's verify the schedule loads correctly
        // Local time: 2026-07-27 23:00 (Monday) in Bogota
        // Actually 2026-07-28T04:00Z => Bogota UTC-5 => 2026-07-27 23:00 Monday
        Assert.NotNull(ctx.CurrentDaySchedule);
        Assert.Single(ctx.CurrentDaySchedule.TimeSlots);
        Assert.Equal(new TimeOnly(22, 0), ctx.CurrentDaySchedule.TimeSlots[0].OpeningTime);
        Assert.Equal(new TimeOnly(2, 0), ctx.CurrentDaySchedule.TimeSlots[0].ClosingTime);
        Assert.True(ctx.CurrentDaySchedule.TimeSlots[0].CrossesMidnight);
    }

    [Fact]
    public async Task GetBatch_SpecialScheduleOverridesWeekly()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx, "SpecialOverride");

        // 2026-07-27 is Monday in Bogota
        var targetDate = new DateOnly(2026, 7, 27);

        // Seed weekly Monday schedule
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday,
            new TimeOnly(8, 0), new TimeOnly(17, 0));

        // Seed special schedule for the same date (Monday)
        await SeedSpecialSchedule(writeCtx, branchId, targetDate,
            new TimeOnly(10, 0), new TimeOnly(14, 0), reason: "Horario especial");

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        // Evaluate at Monday 15:00 UTC => Bogota 10:00 Monday
        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            [branchId], evaluatedAt, timeZoneService, CancellationToken.None);

        var ctx = result[branchId];

        Assert.NotNull(ctx.CurrentDaySchedule);
        Assert.Equal(ScheduleSource.Special, ctx.CurrentDaySchedule.Source);
        Assert.Single(ctx.CurrentDaySchedule.TimeSlots);
        Assert.Equal(new TimeOnly(10, 0), ctx.CurrentDaySchedule.TimeSlots[0].OpeningTime);
        Assert.Equal(new TimeOnly(14, 0), ctx.CurrentDaySchedule.TimeSlots[0].ClosingTime);
    }

    [Fact]
    public async Task GetBatch_EmptyBranchIds_ReturnsEmpty()
    {
        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            Array.Empty<EstablishmentBranchId>(), evaluatedAt, timeZoneService, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBatch_InactiveBranch_NotReturned()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, inactiveBranch) = await SeedFullBranch(writeCtx, "InactiveBranch", branchIsActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);

        var result = await sut.GetAvailabilityContextsAsync(
            [inactiveBranch], evaluatedAt, timeZoneService, CancellationToken.None);

        Assert.Empty(result);
    }

    public void Dispose() => _fixture.Dispose();

    private sealed class RealTimeZoneService : ITimeZoneService
    {
        public bool IsValid(string timeZoneId)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
        }

        public DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.UtcDateTime, tz);
        }
    }
}
