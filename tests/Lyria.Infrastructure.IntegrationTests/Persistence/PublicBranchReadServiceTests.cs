using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Services;
using Lyria.Infrastructure.Persistence;
using Lyria.Infrastructure.Persistence.ReadServices;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence;

public sealed class PublicBranchReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    #region Seed helpers

    private static async Task<EstablishmentCategoryId> SeedCategory(
        LyriaDbContext context, string? name = null, bool isActive = true)
    {
        var id = EstablishmentCategoryId.New();
        var category = EstablishmentCategory.Create(id, name ?? $"Cat-{id.Value:N}", "Desc", null, 1);
        if (!isActive)
        {
            category.Deactivate();
        }

        context.Set<EstablishmentCategory>().Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<EstablishmentId> SeedEstablishment(
        LyriaDbContext context,
        EstablishmentCategoryId categoryId,
        string? name = null,
        string? slug = null,
        bool isActive = true)
    {
        var id = EstablishmentId.New();
        var actualName = name ?? $"Est-{id.Value:N}";
        var actualSlug = slug ?? $"est-{id.Value:N}";
        var establishment = Establishment.Create(
            id, categoryId, actualName, actualSlug, null, null, null, null, null, null);
        if (!isActive)
        {
            establishment.Deactivate();
        }

        context.Set<Establishment>().Add(establishment);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<EstablishmentBranchId> SeedBranch(
        LyriaDbContext context,
        EstablishmentId establishmentId,
        string? name = null,
        bool isActive = true)
    {
        var id = EstablishmentBranchId.New();
        var actualName = name ?? $"Sede-{id.Value:N}";
        var branch = EstablishmentBranch.Create(
            id, establishmentId, actualName, "Calle 1", null, null, null, "Bogota", "Cundinamarca", null, "Colombia", null, null, null, null, null, "America/Argentina/Buenos_Aires");
        if (!isActive)
        {
            branch.Deactivate();
        }

        context.Set<EstablishmentBranch>().Add(branch);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<ServiceId> SeedService(
        LyriaDbContext context, string? name = null, bool isActive = true)
    {
        var id = ServiceId.New();
        var service = Service.Create(id, name ?? $"Srv-{id.Value:N}", "Desc", null);
        if (!isActive)
        {
            service.Deactivate();
        }

        context.Set<Service>().Add(service);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task SeedBranchService(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        bool isAvailable = true,
        bool isActive = true)
    {
        var bs = EstablishmentBranchService.Create(branchId, serviceId, isAvailable, null);
        if (!isActive)
        {
            bs.Deactivate();
        }

        context.Set<EstablishmentBranchService>().Add(bs);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<RestrictionId> SeedRestriction(
        LyriaDbContext context, string? name = null, bool isActive = true)
    {
        var id = RestrictionId.New();
        var restriction = Restriction.Create(id, name ?? $"Restr-{id.Value:N}", "Desc");
        if (!isActive)
        {
            restriction.Deactivate();
        }

        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task SeedBranchRestriction(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        RestrictionComplianceLevel complianceLevel = RestrictionComplianceLevel.Guaranteed,
        bool isCertified = false,
        bool isActive = true)
    {
        var br = EstablishmentBranchRestriction.Create(
            branchId, restrictionId, complianceLevel, isCertified, null);
        if (!isActive)
        {
            br.Deactivate();
        }

        context.Set<EstablishmentBranchRestriction>().Add(br);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<BranchScheduleId> SeedSchedule(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        WeekDay day = WeekDay.Monday,
        bool isActive = true)
    {
        var id = BranchScheduleId.New();
        var schedule = BranchSchedule.Create(
            id, branchId, day, new TimeOnly(8, 0), new TimeOnly(17, 0), false, false);
        if (!isActive)
        {
            schedule.Deactivate();
        }

        context.Set<BranchSchedule>().Add(schedule);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<BranchImageId> SeedImage(
        LyriaDbContext context,
        EstablishmentBranchId branchId,
        bool isPrimary = false,
        bool isActive = true)
    {
        var id = BranchImageId.New();
        var image = BranchImage.Create(
            id, branchId, $"https://cdn.lyria.com/{id.Value}.jpg", $"{id.Value}.jpg", null, isPrimary, 0);
        if (!isActive)
        {
            image.Deactivate();
        }
        context.Set<BranchImage>().Add(image);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<(EstablishmentCategoryId CategoryId, EstablishmentId EstablishmentId, EstablishmentBranchId BranchId)> SeedFullBranch(
        LyriaDbContext context,
        string? branchName = null)
    {
        var catId = await SeedCategory(context);
        var estId = await SeedEstablishment(context, catId);
        var branchId = await SeedBranch(context, estId, branchName);
        return (catId, estId, branchId);
    }

    #endregion

    [Fact]
    public async Task GetByIdAsync_ReturnsBranchWithFullDetails()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (catId, estId, branchId) = await SeedFullBranch(writeCtx, "Sede Principal");

        var serviceId = await SeedService(writeCtx, "Delivery");
        await SeedBranchService(writeCtx, branchId, serviceId);
        var restrictionId = await SeedRestriction(writeCtx, "Sin Gluten");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId, RestrictionComplianceLevel.Guaranteed, true);
        await SeedSchedule(writeCtx, branchId);
        await SeedImage(writeCtx, branchId, isPrimary: true);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(branchId.Value, result.Branch.Id);
        Assert.Equal("Sede Principal", result.Branch.Name);
        Assert.NotNull(result.Establishment);
        Assert.NotNull(result.Establishment.Category);
        Assert.NotNull(result.Branch.Address);
        Assert.NotNull(result.Branch.Contact);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForNonExistentBranch()
    {
        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(EstablishmentBranchId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForInactiveBranch()
    {
        await using var writeCtx = _fixture.CreateContext();
        var catId = await SeedCategory(writeCtx);
        var estId = await SeedEstablishment(writeCtx, catId);
        var branchId = await SeedBranch(writeCtx, estId, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForInactiveEstablishment()
    {
        await using var writeCtx = _fixture.CreateContext();
        var catId = await SeedCategory(writeCtx);
        var estId = await SeedEstablishment(writeCtx, catId, isActive: false);
        var branchId = await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForInactiveCategory()
    {
        await using var writeCtx = _fixture.CreateContext();
        var catId = await SeedCategory(writeCtx, isActive: false);
        var estId = await SeedEstablishment(writeCtx, catId);
        var branchId = await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesServicesRestrictionsSchedulesImages()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx);

        var serviceId = await SeedService(writeCtx, "Parking");
        await SeedBranchService(writeCtx, branchId, serviceId);
        var restrictionId = await SeedRestriction(writeCtx, "Vegano");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId);
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday);
        await SeedImage(writeCtx, branchId, isPrimary: true);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Branch.Services);
        Assert.Equal("Parking", result.Branch.Services[0].Name);
        Assert.Single(result.Branch.Restrictions);
        Assert.Equal("Vegano", result.Branch.Restrictions[0].Name);
        Assert.Single(result.Branch.Schedules);
        Assert.Single(result.Branch.Images);
    }

    [Fact]
    public async Task GetByIdAsync_ExcludesInactiveRelatedData()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullBranch(writeCtx);

        // Active service + inactive service
        var activeSrv = await SeedService(writeCtx, "ActiveSrv");
        await SeedBranchService(writeCtx, branchId, activeSrv, isActive: true);
        var inactiveSrv = await SeedService(writeCtx, "InactiveSrv", isActive: false);
        await SeedBranchService(writeCtx, branchId, inactiveSrv);

        // Inactive branch-service relationship
        var srvWithInactiveRel = await SeedService(writeCtx, "InactiveRelSrv");
        await SeedBranchService(writeCtx, branchId, srvWithInactiveRel, isActive: false);

        // Active restriction + inactive restriction
        var activeRestr = await SeedRestriction(writeCtx, "ActiveRestr");
        await SeedBranchRestriction(writeCtx, branchId, activeRestr, isActive: true);
        var inactiveRestr = await SeedRestriction(writeCtx, "InactiveRestr", isActive: false);
        await SeedBranchRestriction(writeCtx, branchId, inactiveRestr);

        // Inactive branch-restriction relationship
        var restrWithInactiveRel = await SeedRestriction(writeCtx, "InactiveRelRestr");
        await SeedBranchRestriction(writeCtx, branchId, restrWithInactiveRel, isActive: false);

        // Active schedule + inactive schedule
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday, isActive: true);
        await SeedSchedule(writeCtx, branchId, WeekDay.Tuesday, isActive: false);

        // Active image + inactive image
        await SeedImage(writeCtx, branchId, isPrimary: true, isActive: true);
        await SeedImage(writeCtx, branchId, isPrimary: false, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicBranchReadService(readCtx);

        var result = await sut.GetByIdAsync(branchId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Branch.Services);
        Assert.Equal("ActiveSrv", result.Branch.Services[0].Name);
        Assert.Single(result.Branch.Restrictions);
        Assert.Equal("ActiveRestr", result.Branch.Restrictions[0].Name);
        Assert.Single(result.Branch.Schedules);
        Assert.Equal(1, result.Branch.Schedules[0].DayOfWeek); // Monday
        Assert.Single(result.Branch.Images);
        Assert.True(result.Branch.Images[0].IsPrimary);
    }

    public void Dispose() => _fixture.Dispose();
}
