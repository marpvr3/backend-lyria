using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchScheduleRepositoryTests : IDisposable
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
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        context.Set<EstablishmentBranch>().Add(branch);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return branch;
    }

    [Fact]
    public async Task ReplaceSchedules_PersistsNewSchedules()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchScheduleRepository(context);

        var schedules = new List<BranchSchedule>
        {
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false),
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Sunday,
                null, null, false, true)
        };

        await repository.ReplaceSchedulesAsync(branch.Id, schedules,
            TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSchedule>()
            .Where(s => s.BranchId == branch.Id && s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, persisted.Count);
    }

    [Fact]
    public async Task ReplaceSchedules_DeactivatesOldSchedules()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchScheduleRepository(context);

        // First set
        var firstSet = new List<BranchSchedule>
        {
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false)
        };
        await repository.ReplaceSchedulesAsync(branch.Id, firstSet,
            TestContext.Current.CancellationToken);

        // Second set replaces first
        var secondSet = new List<BranchSchedule>
        {
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Tuesday,
                new TimeOnly(10, 0), new TimeOnly(18, 0), false, false)
        };
        await repository.ReplaceSchedulesAsync(branch.Id, secondSet,
            TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var activeSchedules = await readContext.Set<BranchSchedule>()
            .Where(s => s.BranchId == branch.Id && s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        var inactiveSchedules = await readContext.Set<BranchSchedule>()
            .Where(s => s.BranchId == branch.Id && !s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(activeSchedules);
        Assert.Equal(WeekDay.Tuesday, activeSchedules[0].DayOfWeek);
        Assert.Single(inactiveSchedules);
    }

    [Fact]
    public async Task GetActiveByBranchIdAsync_ReturnsOnlyActive()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchScheduleRepository(context);

        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false);
        var inactive = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Tuesday,
            new TimeOnly(10, 0), new TimeOnly(18, 0), false, false);
        inactive.Deactivate();

        context.Set<BranchSchedule>().AddRange(schedule, inactive);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetActiveByBranchIdAsync(branch.Id,
            TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(WeekDay.Monday, result[0].DayOfWeek);
    }

    [Fact]
    public async Task ReplaceSchedules_PersistsTimeOnly()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchScheduleRepository(context);

        var opening = new TimeOnly(22, 30);
        var closing = new TimeOnly(3, 45);

        var schedules = new List<BranchSchedule>
        {
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Friday,
                opening, closing, true, false)
        };

        await repository.ReplaceSchedulesAsync(branch.Id, schedules,
            TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSchedule>()
            .FirstAsync(s => s.BranchId == branch.Id && s.IsActive,
                TestContext.Current.CancellationToken);

        Assert.Equal(opening, persisted.OpeningTime);
        Assert.Equal(closing, persisted.ClosingTime);
        Assert.True(persisted.CrossesMidnight);
    }

    [Fact]
    public async Task Audit_CreatedAtUtc_IsSet()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false);

        context.Set<BranchSchedule>().Add(schedule);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSchedule>()
            .FirstAsync(s => s.Id == schedule.Id,
                TestContext.Current.CancellationToken);

        Assert.NotEqual(default, persisted.CreatedAtUtc);
        Assert.Null(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task Audit_UpdatedAtUtc_IsSetOnModification()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var schedule = BranchSchedule.Create(
            BranchScheduleId.New(), branch.Id, WeekDay.Monday,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false);

        context.Set<BranchSchedule>().Add(schedule);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        schedule.Deactivate();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSchedule>()
            .FirstAsync(s => s.Id == schedule.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetActiveByBranchIdAsync_OrderedByDayAndTime()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSchedule>().AddRange(
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Tuesday,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false),
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(19, 0), new TimeOnly(23, 0), false, false),
            BranchSchedule.Create(
                BranchScheduleId.New(), branch.Id, WeekDay.Monday,
                new TimeOnly(12, 0), new TimeOnly(15, 0), false, false));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BranchScheduleRepository(context);
        var result = await repository.GetActiveByBranchIdAsync(branch.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Equal(WeekDay.Monday, result[0].DayOfWeek);
        Assert.Equal(new TimeOnly(12, 0), result[0].OpeningTime);
        Assert.Equal(WeekDay.Monday, result[1].DayOfWeek);
        Assert.Equal(new TimeOnly(19, 0), result[1].OpeningTime);
        Assert.Equal(WeekDay.Tuesday, result[2].DayOfWeek);
    }

    public void Dispose() => _fixture.Dispose();
}
