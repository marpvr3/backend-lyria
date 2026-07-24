using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchScheduleReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static async Task<EstablishmentBranch> SeedBranch(LyriaDbContext context)
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 1);
        context.Set<EstablishmentCategory>().Add(category);

        var establishment = Establishment.Create(
            EstablishmentId.New(), category.Id, "Let It V", "let-it-v",
            null, null, null, null, null, null);
        context.Set<Establishment>().Add(establishment);

        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(), establishment.Id,
            "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        context.Set<EstablishmentBranch>().Add(branch);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return branch;
    }

    [Fact]
    public async Task BranchExistsAsync_ExistingBranch_ReturnsTrue()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.BranchExistsAsync(branch.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task BranchExistsAsync_NonExistingBranch_ReturnsFalse()
    {
        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.BranchExistsAsync(EstablishmentBranchId.New(),
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_ReturnsAllSevenDays()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSchedule>().Add(
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetWeeklyScheduleAsync(branch.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(7, result.Schedules.Count);
        Assert.Equal(1, result.Schedules[0].DayOfWeek);
        Assert.Equal(7, result.Schedules[6].DayOfWeek);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_GroupsSlotsByDay()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSchedule>().AddRange(
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(12, 0), new TimeOnly(15, 0), false, false),
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(19, 0), new TimeOnly(23, 30), false, false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetWeeklyScheduleAsync(branch.Id,
            TestContext.Current.CancellationToken);

        var monday = result!.Schedules.First(d => d.DayOfWeek == 1);
        Assert.Equal(2, monday.TimeSlots.Count);
        Assert.Equal("12:00", monday.TimeSlots[0].OpeningTime);
        Assert.Equal("19:00", monday.TimeSlots[1].OpeningTime);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_ClosedDay_HasNoTimeSlots()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSchedule>().Add(
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Sunday,
                null, null, false, true));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetWeeklyScheduleAsync(branch.Id,
            TestContext.Current.CancellationToken);

        var sunday = result!.Schedules.First(d => d.DayOfWeek == 7);
        Assert.True(sunday.IsClosed);
        Assert.Empty(sunday.TimeSlots);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_DayNames_InSpanish()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetWeeklyScheduleAsync(branch.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal("Lunes", result!.Schedules[0].DayName);
        Assert.Equal("Domingo", result.Schedules[6].DayName);
    }

    [Fact]
    public async Task GetDayScheduleAsync_ExistingDay_ReturnsSlots()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSchedule>().Add(
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Friday,
                new TimeOnly(22, 0), new TimeOnly(2, 0), true, false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetDayScheduleAsync(branch.Id, WeekDay.Friday,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(5, result.DayOfWeek);
        Assert.Equal("Viernes", result.DayName);
        Assert.Single(result.TimeSlots);
        Assert.True(result.TimeSlots[0].CrossesMidnight);
    }

    [Fact]
    public async Task GetDayScheduleAsync_NoSchedule_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetDayScheduleAsync(branch.Id, WeekDay.Wednesday,
            TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_IgnoresInactiveSchedules()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var active = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false);
        var inactive = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Monday,
            new TimeOnly(18, 0), new TimeOnly(22, 0), false, false);
        inactive.Deactivate();

        context.Set<BranchSchedule>().AddRange(active, inactive);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var readService = new BranchScheduleReadService(readContext);

        var result = await readService.GetWeeklyScheduleAsync(branch.Id,
            TestContext.Current.CancellationToken);

        var monday = result!.Schedules.First(d => d.DayOfWeek == 1);
        Assert.Single(monday.TimeSlots);
    }

    public void Dispose() => _fixture.Dispose();
}
