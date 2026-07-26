using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Lyria.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class BranchSpecialScheduleRepositoryTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    private static readonly DateOnly TestDate = new(2026, 12, 25);

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

    // --- Repository: GetActiveByBranchAndDateAsync ---

    [Fact]
    public async Task GetActiveByBranchAndDate_ReturnsOnlyActiveForDate()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var activeForDate = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, "Feriado");

        var inactiveForDate = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(18, 0), new TimeOnly(22, 0), false, false, null);
        inactiveForDate.Deactivate();

        var activeOtherDate = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate.AddDays(1),
            new TimeOnly(10, 0), new TimeOnly(14, 0), false, false, null);

        context.Set<BranchSpecialSchedule>().AddRange(activeForDate, inactiveForDate, activeOtherDate);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BranchSpecialScheduleRepository(context);
        var result = await repository.GetActiveByBranchAndDateAsync(
            branch.Id, TestDate, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(activeForDate.Id, result[0].Id);
    }

    [Fact]
    public async Task GetActiveByBranchAndDate_OrderedByOpeningTime()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var afternoon = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(14, 0), new TimeOnly(18, 0), false, false, null);

        var morning = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(8, 0), new TimeOnly(12, 0), false, false, null);

        context.Set<BranchSpecialSchedule>().AddRange(afternoon, morning);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BranchSpecialScheduleRepository(context);
        var result = await repository.GetActiveByBranchAndDateAsync(
            branch.Id, TestDate, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal(new TimeOnly(8, 0), result[0].OpeningTime);
        Assert.Equal(new TimeOnly(14, 0), result[1].OpeningTime);
    }

    [Fact]
    public async Task GetActiveByBranchAndDate_ReturnsEmptyWhenNoneMatch()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);
        var result = await repository.GetActiveByBranchAndDateAsync(
            branch.Id, TestDate, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // --- Repository: ReplaceForDateAsync ---

    [Fact]
    public async Task ReplaceForDate_PersistsNewSchedules()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);

        var schedules = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                new TimeOnly(9, 0), new TimeOnly(13, 0), false, false, "Horario especial"),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                new TimeOnly(15, 0), new TimeOnly(20, 0), false, false, null)
        };

        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, schedules, TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branch.Id && s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, persisted.Count);
    }

    [Fact]
    public async Task ReplaceForDate_DeactivatesExistingActiveForSameDate()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);

        // First set
        var firstSet = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, "Primera version")
        };
        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, firstSet, TestContext.Current.CancellationToken);

        // Second set replaces first
        var secondSet = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                new TimeOnly(10, 0), new TimeOnly(18, 0), false, false, "Segunda version")
        };
        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, secondSet, TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var active = await readContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branch.Id && s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        var inactive = await readContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branch.Id && !s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(active);
        Assert.Equal("Segunda version", active[0].Reason);
        Assert.Single(inactive);
    }

    [Fact]
    public async Task ReplaceForDate_DoesNotAffectOtherDates()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);

        var otherDate = TestDate.AddDays(1);

        // Seed a schedule for another date
        var otherDateSchedule = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, otherDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null)
        };
        await repository.ReplaceForDateAsync(
            branch.Id, otherDate, otherDateSchedule, TestContext.Current.CancellationToken);

        // Replace for TestDate only
        var testDateSchedule = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                null, null, false, true, "Cerrado por feriado")
        };
        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, testDateSchedule, TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var otherDateActive = await readContext.Set<BranchSpecialSchedule>()
            .Where(s => s.BranchId == branch.Id && s.Date == otherDate && s.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(otherDateActive);
    }

    [Fact]
    public async Task ReplaceForDate_PersistsClosedDay()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);

        var closedSchedule = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                null, null, false, true, "Cerrado por mantenimiento")
        };

        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, closedSchedule, TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSpecialSchedule>()
            .FirstAsync(s => s.BranchId == branch.Id && s.IsActive,
                TestContext.Current.CancellationToken);

        Assert.True(persisted.IsClosed);
        Assert.Null(persisted.OpeningTime);
        Assert.Null(persisted.ClosingTime);
        Assert.False(persisted.CrossesMidnight);
        Assert.Equal("Cerrado por mantenimiento", persisted.Reason);
    }

    [Fact]
    public async Task ReplaceForDate_PersistsCrossesMidnight()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var repository = new BranchSpecialScheduleRepository(context);

        var opening = new TimeOnly(22, 0);
        var closing = new TimeOnly(4, 0);

        var schedules = new List<BranchSpecialSchedule>
        {
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                opening, closing, true, false, "Evento nocturno")
        };

        await repository.ReplaceForDateAsync(
            branch.Id, TestDate, schedules, TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSpecialSchedule>()
            .FirstAsync(s => s.BranchId == branch.Id && s.IsActive,
                TestContext.Current.CancellationToken);

        Assert.Equal(opening, persisted.OpeningTime);
        Assert.Equal(closing, persisted.ClosingTime);
        Assert.True(persisted.CrossesMidnight);
    }

    // --- Audit ---

    [Fact]
    public async Task Audit_CreatedAtUtc_IsSet()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var schedule = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null);

        context.Set<BranchSpecialSchedule>().Add(schedule);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSpecialSchedule>()
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

        var schedule = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null);

        context.Set<BranchSpecialSchedule>().Add(schedule);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        schedule.Deactivate();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var readContext = _fixture.CreateContext();
        var persisted = await readContext.Set<BranchSpecialSchedule>()
            .FirstAsync(s => s.Id == schedule.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(persisted.UpdatedAtUtc);
    }

    // --- ReadService: BranchExistsAsync ---

    [Fact]
    public async Task BranchExists_ReturnsTrueWhenBranchExists()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.BranchExistsAsync(
            branch.Id, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task BranchExists_ReturnsFalseWhenBranchDoesNotExist()
    {
        using var context = _fixture.CreateContext();

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.BranchExistsAsync(
            new EstablishmentBranchId(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    // --- ReadService: GetByDateRangeAsync ---

    [Fact]
    public async Task GetByDateRange_ReturnsActiveSchedulesInRange()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var fromDate = new DateOnly(2026, 12, 24);
        var toDate = new DateOnly(2026, 12, 26);

        context.Set<BranchSpecialSchedule>().AddRange(
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, new DateOnly(2026, 12, 24),
                new TimeOnly(9, 0), new TimeOnly(14, 0), false, false, "Vispera"),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                null, null, false, true, "Navidad"),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, new DateOnly(2026, 12, 26),
                new TimeOnly(10, 0), new TimeOnly(18, 0), false, false, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, fromDate, toDate, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(branch.Id.Value, result.BranchId);
        Assert.Equal("2026-12-24", result.From);
        Assert.Equal("2026-12-26", result.To);
        Assert.Equal(3, result.Schedules.Count);
    }

    [Fact]
    public async Task GetByDateRange_ExcludesInactiveSchedules()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var active = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null);

        var inactive = BranchSpecialSchedule.Create(
            new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
            new TimeOnly(18, 0), new TimeOnly(22, 0), false, false, null);
        inactive.Deactivate();

        context.Set<BranchSpecialSchedule>().AddRange(active, inactive);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, TestDate, TestDate, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result.Schedules);
        Assert.Single(result.Schedules[0].TimeSlots);
    }

    [Fact]
    public async Task GetByDateRange_GroupsByDate()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var date1 = new DateOnly(2026, 12, 24);
        var date2 = new DateOnly(2026, 12, 25);

        context.Set<BranchSpecialSchedule>().AddRange(
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, date1,
                new TimeOnly(9, 0), new TimeOnly(13, 0), false, false, "Dia 1 slot 1"),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, date1,
                new TimeOnly(15, 0), new TimeOnly(20, 0), false, false, "Dia 1 slot 2"),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, date2,
                new TimeOnly(10, 0), new TimeOnly(14, 0), false, false, "Dia 2 slot 1"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, date1, date2, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(2, result.Schedules.Count);
        Assert.Equal("2026-12-24", result.Schedules[0].Date);
        Assert.Equal(2, result.Schedules[0].TimeSlots.Count);
        Assert.Equal("2026-12-25", result.Schedules[1].Date);
        Assert.Single(result.Schedules[1].TimeSlots);
    }

    [Fact]
    public async Task GetByDateRange_ClosedDay_HasNoTimeSlots()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSpecialSchedule>().Add(
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                null, null, false, true, "Navidad"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, TestDate, TestDate, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result.Schedules);
        Assert.True(result.Schedules[0].IsClosed);
        Assert.Equal("Navidad", result.Schedules[0].Reason);
        Assert.Empty(result.Schedules[0].TimeSlots);
    }

    [Fact]
    public async Task GetByDateRange_ReturnsEmptyDatesWhenNoneMatch()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, TestDate, TestDate, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result.Schedules);
    }

    [Fact]
    public async Task GetByDateRange_TimeSlotsContainFormattedTimes()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        context.Set<BranchSpecialSchedule>().Add(
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, TestDate,
                new TimeOnly(22, 30), new TimeOnly(3, 45), true, false, "Nocturno"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id, TestDate, TestDate, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var slot = result.Schedules[0].TimeSlots[0];
        Assert.Equal("22:30", slot.OpeningTime);
        Assert.Equal("03:45", slot.ClosingTime);
        Assert.True(slot.CrossesMidnight);
    }

    [Fact]
    public async Task GetByDateRange_ExcludesSchedulesOutsideRange()
    {
        using var context = _fixture.CreateContext();
        var branch = await SeedBranch(context);

        var insideDate = new DateOnly(2026, 12, 25);
        var outsideDate = new DateOnly(2026, 12, 30);

        context.Set<BranchSpecialSchedule>().AddRange(
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, insideDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null),
            BranchSpecialSchedule.Create(
                new BranchSpecialScheduleId(Guid.NewGuid()), branch.Id, outsideDate,
                new TimeOnly(9, 0), new TimeOnly(17, 0), false, false, null));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var readService = new BranchSpecialScheduleReadService(context);
        var result = await readService.GetByDateRangeAsync(
            branch.Id,
            new DateOnly(2026, 12, 24),
            new DateOnly(2026, 12, 26),
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result.Schedules);
        Assert.Equal("2026-12-25", result.Schedules[0].Date);
    }

    public void Dispose() => _fixture.Dispose();
}
