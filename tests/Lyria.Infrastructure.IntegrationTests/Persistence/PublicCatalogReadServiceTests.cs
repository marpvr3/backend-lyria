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

public sealed class PublicCatalogReadServiceTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    #region Seed helpers

    private static async Task<EstablishmentCategoryId> SeedCategory(
        LyriaDbContext context,
        string? name = null,
        int sortOrder = 1,
        bool isActive = true)
    {
        var id = EstablishmentCategoryId.New();
        var category = EstablishmentCategory.Create(id, name ?? $"Cat-{id.Value:N}", "Desc", null, sortOrder);
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
        string? city = "Bogota",
        string? province = "Cundinamarca",
        string? country = "Colombia",
        bool isActive = true)
    {
        var id = EstablishmentBranchId.New();
        var branch = EstablishmentBranch.Create(
            id, establishmentId, $"Sede-{id.Value:N}", "Calle 1", null, null, null, city, province, null, country, null, null, null, null, null, "America/Argentina/Buenos_Aires");
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
        var service = Service.Create(id, name ?? $"Srv-{id.Value:N}", null, null);
        if (!isActive)
        {
            service.Deactivate();
        }

        context.Set<Service>().Add(service);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<RestrictionId> SeedRestriction(
        LyriaDbContext context, string? name = null, bool isActive = true)
    {
        var id = RestrictionId.New();
        var restriction = Restriction.Create(id, name ?? $"Restr-{id.Value:N}", null);
        if (!isActive)
        {
            restriction.Deactivate();
        }
        context.Set<Restriction>().Add(restriction);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    #endregion

    [Fact]
    public async Task GetCatalogsAsync_ReturnsActiveCategoriesOnly()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedCategory(writeCtx, $"ActiveCat{unique}");
        await SeedCategory(writeCtx, $"InactiveCat{unique}", isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.Contains(result.Categories, c => c.Name == $"ActiveCat{unique}");
        Assert.DoesNotContain(result.Categories, c => c.Name == $"InactiveCat{unique}");
    }

    [Fact]
    public async Task GetCatalogsAsync_ReturnsActiveServicesOnly()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedService(writeCtx, $"ActiveSrv{unique}");
        await SeedService(writeCtx, $"InactiveSrv{unique}", isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.Contains(result.Services, s => s.Name == $"ActiveSrv{unique}");
        Assert.DoesNotContain(result.Services, s => s.Name == $"InactiveSrv{unique}");
    }

    [Fact]
    public async Task GetCatalogsAsync_ReturnsActiveRestrictionsOnly()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedRestriction(writeCtx, $"ActiveRestr{unique}");
        await SeedRestriction(writeCtx, $"InactiveRestr{unique}", isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.Contains(result.Restrictions, r => r.Name == $"ActiveRestr{unique}");
        Assert.DoesNotContain(result.Restrictions, r => r.Name == $"InactiveRestr{unique}");
    }

    [Fact]
    public async Task GetCatalogsAsync_ExcludesLocationsFromInactiveBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatLocInact{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"EstLocInact{unique}", $"est-loc-inact-{unique}");
        await SeedBranch(writeCtx, estId, city: $"CiudadInactiva{unique}", province: $"ProvInactiva{unique}", country: $"PaisInactivo{unique}", isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.DoesNotContain(result.Locations.Countries, c => c == $"PaisInactivo{unique}");
        Assert.DoesNotContain(result.Locations.Cities, c => c.Name == $"CiudadInactiva{unique}");
    }

    [Fact]
    public async Task GetCatalogsAsync_ExcludesLocationsFromInactiveEstablishments()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatEstInact{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"EstInact{unique}", $"est-inact-{unique}", isActive: false);
        await SeedBranch(writeCtx, estId, city: $"CiudadEstInact{unique}", province: $"ProvEstInact{unique}", country: $"PaisEstInact{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.DoesNotContain(result.Locations.Countries, c => c == $"PaisEstInact{unique}");
        Assert.DoesNotContain(result.Locations.Cities, c => c.Name == $"CiudadEstInact{unique}");
    }

    [Fact]
    public async Task GetCatalogsAsync_OrdersCategoriesBySortOrderThenName()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedCategory(writeCtx, $"Z{unique}", sortOrder: 1);
        await SeedCategory(writeCtx, $"A{unique}", sortOrder: 1);
        await SeedCategory(writeCtx, $"M{unique}", sortOrder: 0);

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        // Filter to our test categories
        var ours = result.Categories.Where(c => c.Name.EndsWith(unique, StringComparison.Ordinal)).ToList();
        Assert.Equal(3, ours.Count);
        // SortOrder 0 first, then SortOrder 1 by name
        Assert.Equal($"M{unique}", ours[0].Name);
        Assert.Equal($"A{unique}", ours[1].Name);
        Assert.Equal($"Z{unique}", ours[2].Name);
    }

    [Fact]
    public async Task GetCatalogsAsync_OrdersServicesByName()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedService(writeCtx, $"Z{unique}");
        await SeedService(writeCtx, $"A{unique}");
        await SeedService(writeCtx, $"M{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        var ours = result.Services.Where(s => s.Name.EndsWith(unique, StringComparison.Ordinal)).ToList();
        Assert.Equal(3, ours.Count);
        Assert.Equal($"A{unique}", ours[0].Name);
        Assert.Equal($"M{unique}", ours[1].Name);
        Assert.Equal($"Z{unique}", ours[2].Name);
    }

    [Fact]
    public async Task GetCatalogsAsync_OrdersRestrictionsByName()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        await SeedRestriction(writeCtx, $"Z{unique}");
        await SeedRestriction(writeCtx, $"A{unique}");
        await SeedRestriction(writeCtx, $"M{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        var ours = result.Restrictions.Where(r => r.Name.EndsWith(unique, StringComparison.Ordinal)).ToList();
        Assert.Equal(3, ours.Count);
        Assert.Equal($"A{unique}", ours[0].Name);
        Assert.Equal($"M{unique}", ours[1].Name);
        Assert.Equal($"Z{unique}", ours[2].Name);
    }

    [Fact]
    public async Task GetCatalogsAsync_EliminatesDuplicateLocations()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatDup{unique}");

        // Two establishments with branches in the same city
        var est1 = await SeedEstablishment(writeCtx, catId, $"Est1Dup{unique}", $"est1-dup-{unique}");
        await SeedBranch(writeCtx, est1, city: $"CiudadDup{unique}", province: $"ProvDup{unique}", country: $"PaisDup{unique}");

        var est2 = await SeedEstablishment(writeCtx, catId, $"Est2Dup{unique}", $"est2-dup-{unique}");
        await SeedBranch(writeCtx, est2, city: $"CiudadDup{unique}", province: $"ProvDup{unique}", country: $"PaisDup{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        var result = await sut.GetCatalogsAsync(CancellationToken.None);

        var matchingCountries = result.Locations.Countries.Count(c => c == $"PaisDup{unique}");
        Assert.Equal(1, matchingCountries);

        var matchingCities = result.Locations.Cities.Count(c => c.Name == $"CiudadDup{unique}");
        Assert.Equal(1, matchingCities);

        var matchingProvinces = result.Locations.Provinces.Count(p => p.Name == $"ProvDup{unique}");
        Assert.Equal(1, matchingProvinces);
    }

    [Fact]
    public async Task GetCatalogsAsync_UsesAsNoTracking()
    {
        await using var writeCtx = _fixture.CreateContext();
        await SeedCategory(writeCtx, "TrackedCat");
        await SeedService(writeCtx, "TrackedSrv");
        await SeedRestriction(writeCtx, "TrackedRestr");

        await using var readCtx = _fixture.CreateContext();
        var sut = new PublicCatalogReadService(readCtx);

        await sut.GetCatalogsAsync(CancellationToken.None);

        Assert.Empty(readCtx.ChangeTracker.Entries());
    }

    public void Dispose() => _fixture.Dispose();
}
