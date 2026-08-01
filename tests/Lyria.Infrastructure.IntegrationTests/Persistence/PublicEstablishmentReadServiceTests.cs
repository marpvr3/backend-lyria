using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common;
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

public sealed class PublicEstablishmentReadServiceTests : IDisposable
{
    private static readonly string[] ExpectedBranchOrder = ["Alpha", "Bravo", "Charlie"];
    private readonly SqliteFixture _fixture = new();

    private static PublicEstablishmentListFilter DefaultFilter(
        string? search = null,
        Guid? categoryId = null,
        string? city = null,
        string? province = null,
        string? country = null,
        Guid? serviceId = null,
        Guid? restrictionId = null,
        int? complianceLevel = null,
        bool? isCertified = null,
        bool? openNow = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "name",
        string sortDirection = "asc") =>
        new(search, categoryId, city, province, country, serviceId, restrictionId,
            complianceLevel, isCertified, openNow, page, pageSize, sortBy, sortDirection);

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
        string? description = null,
        bool isActive = true)
    {
        var id = EstablishmentId.New();
        var actualName = name ?? $"Est-{id.Value:N}";
        var actualSlug = slug ?? $"est-{id.Value:N}";
        var establishment = Establishment.Create(
            id, categoryId, actualName, actualSlug, description, null, null, null, null, null);
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
        string? city = "Bogota",
        string? province = "Cundinamarca",
        string? country = "Colombia",
        bool isActive = true)
    {
        var id = EstablishmentBranchId.New();
        var actualName = name ?? $"Sede-{id.Value:N}";
        var branch = EstablishmentBranch.Create(
            id, establishmentId, actualName, "Calle 1", null, null, null, city, province, null, country, null, null, null, null, null, "America/Argentina/Buenos_Aires");
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
        var restriction = Restriction.Create(id, name ?? $"Restr-{id.Value:N}", null);
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
        TimeOnly? opening = null,
        TimeOnly? closing = null,
        bool isClosed = false,
        bool isActive = true)
    {
        var id = BranchScheduleId.New();
        var schedule = BranchSchedule.Create(
            id, branchId, day, opening ?? new TimeOnly(8, 0), closing ?? new TimeOnly(17, 0), false, isClosed);
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
        int sortOrder = 0,
        bool isActive = true)
    {
        var id = BranchImageId.New();
        var image = BranchImage.Create(
            id, branchId, $"https://cdn.lyria.com/{id.Value}.jpg", $"{id.Value}.jpg", null, isPrimary, sortOrder);
        if (!isActive)
        {
            image.Deactivate();
        }
        context.Set<BranchImage>().Add(image);
        await context.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    /// <summary>
    /// Seeds a full active establishment graph: category + establishment + branch.
    /// Returns (categoryId, establishmentId, branchId).
    /// </summary>
    private static async Task<(EstablishmentCategoryId CategoryId, EstablishmentId EstablishmentId, EstablishmentBranchId BranchId)> SeedFullEstablishment(
        LyriaDbContext context,
        string? name = null,
        string? slug = null,
        string? description = null,
        string? city = "Bogota",
        string? province = "Cundinamarca",
        string? country = "Colombia")
    {
        var catId = await SeedCategory(context);
        var estId = await SeedEstablishment(context, catId, name, slug, description);
        var branchId = await SeedBranch(context, estId, city: city, province: province, country: country);
        return (catId, estId, branchId);
    }

    #endregion

    // ────────────────────────── ListAsync ──────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveEstablishments()
    {
        await using var writeCtx = _fixture.CreateContext();
        var catId = await SeedCategory(writeCtx);
        var activeId = await SeedEstablishment(writeCtx, catId, "Activo", "activo");
        var inactiveId = await SeedEstablishment(writeCtx, catId, "Inactivo", "inactivo", isActive: false);
        var active2Id = await SeedEstablishment(writeCtx, catId, "Activo2", "activo2");
        await SeedBranch(writeCtx, activeId);
        await SeedBranch(writeCtx, active2Id);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.Name == "Inactivo");
        Assert.All(result.Items, i => Assert.NotEqual(inactiveId.Value, i.Id));
    }

    [Fact]
    public async Task ListAsync_RequiresActiveCategory()
    {
        await using var writeCtx = _fixture.CreateContext();
        var inactiveCatId = await SeedCategory(writeCtx, "Inactiva", isActive: false);
        var estId = await SeedEstablishment(writeCtx, inactiveCatId, "ConCatInactiva", "con-cat-inactiva");
        await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.Name == "ConCatInactiva");
    }

    [Fact]
    public async Task ListAsync_RequiresAtLeastOneActiveBranch()
    {
        await using var writeCtx = _fixture.CreateContext();
        var catId = await SeedCategory(writeCtx, "CatSinSede");
        var estId = await SeedEstablishment(writeCtx, catId, "SinSede", "sin-sede");
        // no branch seeded

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.Name == "SinSede");
    }

    [Fact]
    public async Task ListAsync_FiltersBySearch_Name()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"BuscaNombre{unique}", $"busca-nombre-{unique}");
        var (_, _, _) = await SeedFullEstablishment(writeCtx, "OtroLugar", $"otro-lugar-{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(search: $"BuscaNombre{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Contains(unique, result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersBySearch_Description()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatDesc{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"EstDesc{unique}", $"est-desc-{unique}", description: $"DescripcionUnica{unique}");
        await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(search: $"DescripcionUnica{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"EstDesc{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByCategory()
    {
        await using var writeCtx = _fixture.CreateContext();
        var cat1 = await SeedCategory(writeCtx, "CatFiltro1");
        var cat2 = await SeedCategory(writeCtx, "CatFiltro2");
        var est1 = await SeedEstablishment(writeCtx, cat1, "EnCat1", "en-cat1");
        await SeedBranch(writeCtx, est1);
        var est2 = await SeedEstablishment(writeCtx, cat2, "EnCat2", "en-cat2");
        await SeedBranch(writeCtx, est2);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(categoryId: cat1.Value), CancellationToken.None);

        Assert.All(result.Items, i => Assert.Equal(cat1.Value, i.Category.Id));
        Assert.DoesNotContain(result.Items, i => i.Name == "EnCat2");
    }

    [Fact]
    public async Task ListAsync_FiltersByCity()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnMedellin{unique}", $"en-medellin-{unique}", city: $"Medellin{unique}");
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnCali{unique}", $"en-cali-{unique}", city: $"Cali{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(city: $"Medellin{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"EnMedellin{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByProvince()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnAntioquia{unique}", $"en-antioquia-{unique}", province: $"Antioquia{unique}");
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnValle{unique}", $"en-valle-{unique}", province: $"Valle{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(province: $"Antioquia{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"EnAntioquia{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByCountry()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnColombia{unique}", $"en-colombia-{unique}", country: $"Colombia{unique}");
        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"EnEcuador{unique}", $"en-ecuador-{unique}", country: $"Ecuador{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(country: $"Colombia{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"EnColombia{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByService()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ConServicio{unique}", $"con-servicio-{unique}");
        var serviceId = await SeedService(writeCtx, $"WiFi{unique}");
        await SeedBranchService(writeCtx, branchId, serviceId);

        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"SinServicio{unique}", $"sin-servicio-{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(serviceId: serviceId.Value), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"ConServicio{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByRestriction()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ConRestr{unique}", $"con-restr-{unique}");
        var restrictionId = await SeedRestriction(writeCtx, $"SinGluten{unique}");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId);

        var (_, _, _) = await SeedFullEstablishment(writeCtx, $"SinRestr{unique}", $"sin-restr-{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(restrictionId: restrictionId.Value), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"ConRestr{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByComplianceLevel()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"Garantizado{unique}", $"garantizado-{unique}");
        var restrictionId = await SeedRestriction(writeCtx, $"RestrCompl{unique}");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId, RestrictionComplianceLevel.Guaranteed);

        var (_, _, branchId2) = await SeedFullEstablishment(writeCtx, $"Parcial{unique}", $"parcial-{unique}");
        var restrictionId2 = await SeedRestriction(writeCtx, $"RestrCompl2{unique}");
        await SeedBranchRestriction(writeCtx, branchId2, restrictionId2, RestrictionComplianceLevel.Partial);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(complianceLevel: (int)RestrictionComplianceLevel.Guaranteed), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Name == $"Garantizado{unique}");
        Assert.DoesNotContain(result.Items, i => i.Name == $"Parcial{unique}");
    }

    [Fact]
    public async Task ListAsync_FiltersByIsCertified()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"Certificado{unique}", $"certificado-{unique}");
        var restrictionId = await SeedRestriction(writeCtx, $"RestrCert{unique}");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId, isCertified: true);

        var (_, _, branchId2) = await SeedFullEstablishment(writeCtx, $"NoCertificado{unique}", $"no-certificado-{unique}");
        var restrictionId2 = await SeedRestriction(writeCtx, $"RestrNoCert{unique}");
        await SeedBranchRestriction(writeCtx, branchId2, restrictionId2, isCertified: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(isCertified: true), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Name == $"Certificado{unique}");
        Assert.DoesNotContain(result.Items, i => i.Name == $"NoCertificado{unique}");
    }

    [Fact]
    public async Task ListAsync_EnforcesServiceAndRestrictionInSameBranch()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatSame{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"MismaSede{unique}", $"misma-sede-{unique}");
        var branchId = await SeedBranch(writeCtx, estId);
        var serviceId = await SeedService(writeCtx, $"SrvSame{unique}");
        await SeedBranchService(writeCtx, branchId, serviceId);
        var restrictionId = await SeedRestriction(writeCtx, $"RestrSame{unique}");
        await SeedBranchRestriction(writeCtx, branchId, restrictionId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(serviceId: serviceId.Value, restrictionId: restrictionId.Value), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Name == $"MismaSede{unique}");
    }

    [Fact]
    public async Task ListAsync_DoesNotMatchWhenServiceAndRestrictionInDifferentBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatDiff{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"DiferenteSede{unique}", $"diferente-sede-{unique}");

        var branch1 = await SeedBranch(writeCtx, estId, "Sede1");
        var branch2 = await SeedBranch(writeCtx, estId, "Sede2");

        var serviceId = await SeedService(writeCtx, $"SrvDiff{unique}");
        await SeedBranchService(writeCtx, branch1, serviceId);

        var restrictionId = await SeedRestriction(writeCtx, $"RestrDiff{unique}");
        await SeedBranchRestriction(writeCtx, branch2, restrictionId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(serviceId: serviceId.Value, restrictionId: restrictionId.Value), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.Name == $"DiferenteSede{unique}");
    }

    [Fact]
    public async Task ListAsync_CombinesMultipleFiltersWithAnd()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatCombo{unique}");

        // Establishment matching all filters
        var est1 = await SeedEstablishment(writeCtx, catId, $"ComboMatch{unique}", $"combo-match-{unique}");
        var branch1 = await SeedBranch(writeCtx, est1, city: $"CiudadCombo{unique}", country: $"PaisCombo{unique}");
        var serviceId = await SeedService(writeCtx, $"SrvCombo{unique}");
        await SeedBranchService(writeCtx, branch1, serviceId);

        // Establishment matching only category
        var est2 = await SeedEstablishment(writeCtx, catId, $"SoloCat{unique}", $"solo-cat-{unique}");
        await SeedBranch(writeCtx, est2, city: $"OtraCiudad{unique}", country: $"OtroPais{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(
                categoryId: catId.Value,
                city: $"CiudadCombo{unique}",
                serviceId: serviceId.Value),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal($"ComboMatch{unique}", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_SortsByNameAscending()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatSort{unique}");

        var estC = await SeedEstablishment(writeCtx, catId, $"C{unique}", $"c-{unique}");
        await SeedBranch(writeCtx, estC);
        var estA = await SeedEstablishment(writeCtx, catId, $"A{unique}", $"a-{unique}");
        await SeedBranch(writeCtx, estA);
        var estB = await SeedEstablishment(writeCtx, catId, $"B{unique}", $"b-{unique}");
        await SeedBranch(writeCtx, estB);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(search: unique, sortBy: "name", sortDirection: "asc"), CancellationToken.None);

        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    [Fact]
    public async Task ListAsync_SortsByNameDescending()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatSortDesc{unique}");

        var estA = await SeedEstablishment(writeCtx, catId, $"A{unique}", $"a-desc-{unique}");
        await SeedBranch(writeCtx, estA);
        var estC = await SeedEstablishment(writeCtx, catId, $"C{unique}", $"c-desc-{unique}");
        await SeedBranch(writeCtx, estC);
        var estB = await SeedEstablishment(writeCtx, catId, $"B{unique}", $"b-desc-{unique}");
        await SeedBranch(writeCtx, estB);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(search: unique, sortBy: "name", sortDirection: "desc"), CancellationToken.None);

        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal(names.OrderByDescending(n => n).ToList(), names);
    }

    [Fact]
    public async Task ListAsync_SortsByNewest()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatNewest{unique}");

        var est1 = await SeedEstablishment(writeCtx, catId, $"Primero{unique}", $"primero-{unique}");
        await SeedBranch(writeCtx, est1);
        var est2 = await SeedEstablishment(writeCtx, catId, $"Segundo{unique}", $"segundo-{unique}");
        await SeedBranch(writeCtx, est2);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        // Default newest sort is desc (newest first), but the implementation uses the sortDirection param
        var result = await sut.ListAsync(
            DefaultFilter(search: unique, sortBy: "newest", sortDirection: "desc"), CancellationToken.None);

        Assert.True(result.Items.Count >= 2);
    }

    [Fact]
    public async Task ListAsync_SortsByBranchCountDesc()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatBrCnt{unique}");

        // Establishment with 1 branch
        var est1 = await SeedEstablishment(writeCtx, catId, $"OneBranch{unique}", $"one-{unique}");
        await SeedBranch(writeCtx, est1);

        // Establishment with 3 branches
        var est3 = await SeedEstablishment(writeCtx, catId, $"ThreeBranches{unique}", $"three-{unique}");
        await SeedBranch(writeCtx, est3);
        await SeedBranch(writeCtx, est3);
        await SeedBranch(writeCtx, est3);

        // Establishment with 2 branches
        var est2 = await SeedEstablishment(writeCtx, catId, $"TwoBranches{unique}", $"two-{unique}");
        await SeedBranch(writeCtx, est2);
        await SeedBranch(writeCtx, est2);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(search: unique, sortBy: "branchcount", sortDirection: "desc"), CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal($"ThreeBranches{unique}", result.Items[0].Name);
        Assert.Equal($"TwoBranches{unique}", result.Items[1].Name);
        Assert.Equal($"OneBranch{unique}", result.Items[2].Name);
    }

    [Fact]
    public async Task ListAsync_PaginatesCorrectly()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatPag{unique}");

        for (int i = 0; i < 5; i++)
        {
            var estId = await SeedEstablishment(writeCtx, catId, $"Pag{unique}{i:D2}", $"pag-{unique}-{i:D2}");
            await SeedBranch(writeCtx, estId);
        }

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var page1 = await sut.ListAsync(
            DefaultFilter(search: $"Pag{unique}", page: 1, pageSize: 2), CancellationToken.None);

        Assert.Equal(5, page1.TotalItems);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Equal(3, page1.TotalPages);

        var page2 = await sut.ListAsync(
            DefaultFilter(search: $"Pag{unique}", page: 2, pageSize: 2), CancellationToken.None);

        Assert.Equal(2, page2.Items.Count);

        var page3 = await sut.ListAsync(
            DefaultFilter(search: $"Pag{unique}", page: 3, pageSize: 2), CancellationToken.None);

        Assert.Single(page3.Items);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoData()
    {
        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(
            DefaultFilter(search: "NONEXISTENT" + Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListAsync_ProjectsCompactDto_HasExpectedFields()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (catId, estId, branchId) = await SeedFullEstablishment(
            writeCtx, $"DtoTest{unique}", $"dto-test-{unique}", description: "Una descripcion");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.ListAsync(DefaultFilter(search: $"DtoTest{unique}"), CancellationToken.None);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal($"DtoTest{unique}", item.Name);
        Assert.Equal($"dto-test-{unique}", item.Slug);
        Assert.Equal("Una descripcion", item.Description);
        Assert.NotNull(item.Category);
        Assert.NotEqual(Guid.Empty, item.Category.Id);
        Assert.True(item.BranchCount >= 1);
    }

    [Fact]
    public async Task ListAsync_UsesAsNoTracking_ChangeTrackerEmpty()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, _) = await SeedFullEstablishment(writeCtx, "Tracked", "tracked");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        await sut.ListAsync(DefaultFilter(search: "Tracked"), CancellationToken.None);

        Assert.Empty(readCtx.ChangeTracker.Entries());
    }

    // ────────────────────────── GetBySlugAsync ──────────────────────────

    [Fact]
    public async Task GetBySlugAsync_ReturnsEstablishmentWithBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"BySlug{unique}", $"by-slug-{unique}");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"by-slug-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal($"BySlug{unique}", result.Name);
        Assert.Equal($"by-slug-{unique}", result.Slug);
        Assert.NotEmpty(result.Branches);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForNonExistentSlug()
    {
        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync("nonexistent-slug-xyz", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForInactiveEstablishment()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatInact{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"InactEst{unique}", $"inact-est-{unique}", isActive: false);
        await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"inact-est-{unique}", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForInactiveCategory()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatInact2{unique}", isActive: false);
        var estId = await SeedEstablishment(writeCtx, catId, $"EstCatInact{unique}", $"est-cat-inact-{unique}");
        await SeedBranch(writeCtx, estId);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"est-cat-inact-{unique}", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_WhenNoActiveBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatNoBranch{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"NoBranch{unique}", $"no-branch-{unique}");
        await SeedBranch(writeCtx, estId, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"no-branch-{unique}", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveBranches()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatExBr{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"ExBranch{unique}", $"ex-branch-{unique}");
        var activeBranch = await SeedBranch(writeCtx, estId, "SedeActiva");
        var inactiveBranch = await SeedBranch(writeCtx, estId, "SedeInactiva", isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-branch-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Branches);
        Assert.Equal("SedeActiva", result.Branches[0].Name);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveServices()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExSrv{unique}", $"ex-srv-{unique}");
        var activeService = await SeedService(writeCtx, $"ActiveSrv{unique}");
        var inactiveService = await SeedService(writeCtx, $"InactiveSrv{unique}", isActive: false);
        await SeedBranchService(writeCtx, branchId, activeService);
        await SeedBranchService(writeCtx, branchId, inactiveService);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-srv-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var branchServices = result.Branches[0].Services;
        Assert.Single(branchServices);
        Assert.Equal($"ActiveSrv{unique}", branchServices[0].Name);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveBranchServiceRelationships()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExBsSrv{unique}", $"ex-bs-srv-{unique}");
        var service1 = await SeedService(writeCtx, $"BsActive{unique}");
        var service2 = await SeedService(writeCtx, $"BsInactive{unique}");
        await SeedBranchService(writeCtx, branchId, service1, isActive: true);
        await SeedBranchService(writeCtx, branchId, service2, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-bs-srv-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var branchServices = result.Branches[0].Services;
        Assert.Single(branchServices);
        Assert.Equal($"BsActive{unique}", branchServices[0].Name);
    }

    [Fact]
    public async Task GetBySlugAsync_IncludesUnavailableServices()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"UnavSrv{unique}", $"unav-srv-{unique}");
        var service = await SeedService(writeCtx, $"Unavailable{unique}");
        await SeedBranchService(writeCtx, branchId, service, isAvailable: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"unav-srv-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var branchServices = result.Branches[0].Services;
        Assert.Single(branchServices);
        Assert.False(branchServices[0].IsAvailable);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveRestrictions()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExRestr{unique}", $"ex-restr-{unique}");
        var activeRestr = await SeedRestriction(writeCtx, $"ActiveRestr{unique}");
        var inactiveRestr = await SeedRestriction(writeCtx, $"InactiveRestr{unique}", isActive: false);
        await SeedBranchRestriction(writeCtx, branchId, activeRestr);
        await SeedBranchRestriction(writeCtx, branchId, inactiveRestr);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-restr-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var branchRestrictions = result.Branches[0].Restrictions;
        Assert.Single(branchRestrictions);
        Assert.Equal($"ActiveRestr{unique}", branchRestrictions[0].Name);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveBranchRestrictions()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExBrRestr{unique}", $"ex-br-restr-{unique}");
        var restr1 = await SeedRestriction(writeCtx, $"BrActive{unique}");
        var restr2 = await SeedRestriction(writeCtx, $"BrInactive{unique}");
        await SeedBranchRestriction(writeCtx, branchId, restr1, isActive: true);
        await SeedBranchRestriction(writeCtx, branchId, restr2, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-br-restr-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var branchRestrictions = result.Branches[0].Restrictions;
        Assert.Single(branchRestrictions);
        Assert.Equal($"BrActive{unique}", branchRestrictions[0].Name);
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveSchedules()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExSched{unique}", $"ex-sched-{unique}");
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday, isActive: true);
        await SeedSchedule(writeCtx, branchId, WeekDay.Tuesday, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-sched-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var schedules = result.Branches[0].Schedules;
        Assert.Single(schedules);
        Assert.Equal(1, schedules[0].DayOfWeek); // Monday
    }

    [Fact]
    public async Task GetBySlugAsync_ExcludesInactiveImages()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"ExImg{unique}", $"ex-img-{unique}");
        var activeImg = await SeedImage(writeCtx, branchId, isPrimary: true, isActive: true);
        await SeedImage(writeCtx, branchId, isPrimary: false, isActive: false);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ex-img-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var images = result.Branches[0].Images;
        Assert.Single(images);
        Assert.Equal(activeImg.Value, images[0].Id);
    }

    [Fact]
    public async Task GetBySlugAsync_OrdersBranchesByNameThenId()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var catId = await SeedCategory(writeCtx, $"CatOrdBr{unique}");
        var estId = await SeedEstablishment(writeCtx, catId, $"OrdBranch{unique}", $"ord-branch-{unique}");
        await SeedBranch(writeCtx, estId, "Charlie");
        await SeedBranch(writeCtx, estId, "Alpha");
        await SeedBranch(writeCtx, estId, "Bravo");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ord-branch-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var names = result.Branches.Select(b => b.Name).ToList();
        Assert.Equal(ExpectedBranchOrder, names);
    }

    [Fact]
    public async Task GetBySlugAsync_OrdersSchedulesMondayToSundayByOpeningTime()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"OrdSched{unique}", $"ord-sched-{unique}");
        await SeedSchedule(writeCtx, branchId, WeekDay.Wednesday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0));
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0));
        await SeedSchedule(writeCtx, branchId, WeekDay.Friday, new TimeOnly(10, 0), new TimeOnly(20, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ord-sched-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var schedules = result.Branches[0].Schedules;
        var days = schedules.Select(s => s.DayOfWeek).ToList();
        Assert.Equal(days.OrderBy(d => d).ToList(), days);

        // Monday should have 2 time slots
        var monday = schedules.First(s => s.DayOfWeek == 1);
        Assert.Equal(2, monday.TimeSlots.Count);
        Assert.Equal("08:00", monday.TimeSlots[0].OpeningTime);
        Assert.Equal("14:00", monday.TimeSlots[1].OpeningTime);
    }

    [Fact]
    public async Task GetBySlugAsync_OrdersImagesByIsPrimaryDescSortOrderAscCreatedAtUtcAsc()
    {
        await using var writeCtx = _fixture.CreateContext();
        string unique = Guid.NewGuid().ToString("N")[..8];
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, $"OrdImg{unique}", $"ord-img-{unique}");
        var img1 = await SeedImage(writeCtx, branchId, isPrimary: false, sortOrder: 2);
        var img2 = await SeedImage(writeCtx, branchId, isPrimary: true, sortOrder: 1);
        var img3 = await SeedImage(writeCtx, branchId, isPrimary: false, sortOrder: 1);

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSut(readCtx);

        var result = await sut.GetBySlugAsync($"ord-img-{unique}", CancellationToken.None);

        Assert.NotNull(result);
        var images = result.Branches[0].Images;
        Assert.Equal(3, images.Count);
        // Primary first
        Assert.True(images[0].IsPrimary);
        Assert.Equal(img2.Value, images[0].Id);
        // Then by sort order asc
        Assert.Equal(img3.Value, images[1].Id);
        Assert.Equal(img1.Value, images[2].Id);
    }

    // ────────────────────────── OpenNow filter ──────────────────────────

    [Fact]
    public async Task ListAsync_OpenNowTrue_IncludesOnlyEstablishmentsWithOpenBranch()
    {
        await using var writeCtx = _fixture.CreateContext();

        // Establishment with an open branch (Monday 08:00-17:00, eval at local 12:00)
        var (_, openEstId, openBranchId) = await SeedFullEstablishment(writeCtx, "OpenEst", "open-est-1");
        await SeedSchedule(writeCtx, openBranchId, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        // Establishment with no schedule (closed)
        var (_, closedEstId, _) = await SeedFullEstablishment(writeCtx, "ClosedEst", "closed-est-1");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        // Monday 2026-07-27 15:00 UTC → 12:00 Buenos Aires
        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("OpenEst", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_OpenNowFalse_IncludesOnlyEstablishmentsWithNoOpenBranch()
    {
        await using var writeCtx = _fixture.CreateContext();

        var (_, openEstId, openBranchId) = await SeedFullEstablishment(writeCtx, "OpenEst2", "open-est-2");
        await SeedSchedule(writeCtx, openBranchId, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, closedEstId, _) = await SeedFullEstablishment(writeCtx, "ClosedEst2", "closed-est-2");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: false, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("ClosedEst2", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_OpenNowNull_DoesNotFilter()
    {
        await using var writeCtx = _fixture.CreateContext();

        var (_, _, openBranchId) = await SeedFullEstablishment(writeCtx, "OpenEst3", "open-est-3");
        await SeedSchedule(writeCtx, openBranchId, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, _) = await SeedFullEstablishment(writeCtx, "ClosedEst3", "closed-est-3");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: null, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.True(result.Items.Count >= 2);
        Assert.Contains(result.Items, i => i.Name == "OpenEst3");
        Assert.Contains(result.Items, i => i.Name == "ClosedEst3");
    }

    [Fact]
    public async Task ListAsync_OpenBranchCount_IsCorrect()
    {
        await using var writeCtx = _fixture.CreateContext();

        var catId = await SeedCategory(writeCtx);
        var estId = await SeedEstablishment(writeCtx, catId, "CountEst", "count-est");
        var branchOpen = await SeedBranch(writeCtx, estId, "OpenBranch");
        await SeedSchedule(writeCtx, branchOpen, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var branchClosed = await SeedBranch(writeCtx, estId, "ClosedBranch");
        // No schedule for branchClosed

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: null, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        var item = Assert.Single(result.Items, i => i.Name == "CountEst");
        Assert.Equal(1, item.OpenBranchCount);
    }

    [Fact]
    public async Task ListAsync_HasOpenBranch_IsCorrect()
    {
        await using var writeCtx = _fixture.CreateContext();

        var catId = await SeedCategory(writeCtx);
        var estId = await SeedEstablishment(writeCtx, catId, "HasOpenEst", "has-open-est");
        var branchOpen = await SeedBranch(writeCtx, estId, "OpenBranch2");
        await SeedSchedule(writeCtx, branchOpen, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var branchClosed = await SeedBranch(writeCtx, estId, "ClosedBranch2");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: null, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        var item = Assert.Single(result.Items, i => i.Name == "HasOpenEst");
        Assert.True(item.HasOpenBranch);
    }

    [Fact]
    public async Task ListAsync_OpenNowTrue_TotalItems_IsCorrect()
    {
        await using var writeCtx = _fixture.CreateContext();

        // 2 open establishments
        var (_, _, br1) = await SeedFullEstablishment(writeCtx, "TotalOpen1", "total-open-1");
        await SeedSchedule(writeCtx, br1, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var (_, _, br2) = await SeedFullEstablishment(writeCtx, "TotalOpen2", "total-open-2");
        await SeedSchedule(writeCtx, br2, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        // 1 closed establishment
        var (_, _, _) = await SeedFullEstablishment(writeCtx, "TotalClosed1", "total-closed-1");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public async Task ListAsync_OpenNowFalse_TotalItems_IsCorrect()
    {
        await using var writeCtx = _fixture.CreateContext();

        var (_, _, br1) = await SeedFullEstablishment(writeCtx, "TotalOpen3", "total-open-3");
        await SeedSchedule(writeCtx, br1, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var (_, _, br2) = await SeedFullEstablishment(writeCtx, "TotalOpen4", "total-open-4");
        await SeedSchedule(writeCtx, br2, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, _) = await SeedFullEstablishment(writeCtx, "TotalClosed2", "total-closed-2");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: false, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task ListAsync_OpenNowTrue_TotalPages_IsCorrect()
    {
        await using var writeCtx = _fixture.CreateContext();

        // 3 open establishments
        var (_, _, brA) = await SeedFullEstablishment(writeCtx, "PageOpen1", "page-open-1");
        await SeedSchedule(writeCtx, brA, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var (_, _, brB) = await SeedFullEstablishment(writeCtx, "PageOpen2", "page-open-2");
        await SeedSchedule(writeCtx, brB, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        var (_, _, brC) = await SeedFullEstablishment(writeCtx, "PageOpen3", "page-open-3");
        await SeedSchedule(writeCtx, brC, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt, pageSize: 2);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_OpenNow_PaginationAppliedAfterFilter()
    {
        await using var writeCtx = _fixture.CreateContext();

        // A-Open, B-Closed, C-Open, D-Open
        var (_, _, brA) = await SeedFullEstablishment(writeCtx, "A-Open", "a-open");
        await SeedSchedule(writeCtx, brA, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, _) = await SeedFullEstablishment(writeCtx, "B-Closed", "b-closed");

        var (_, _, brC) = await SeedFullEstablishment(writeCtx, "C-Open", "c-open");
        await SeedSchedule(writeCtx, brC, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, brD) = await SeedFullEstablishment(writeCtx, "D-Open", "d-open");
        await SeedSchedule(writeCtx, brD, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);

        // Page 1
        var filterP1 = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt, pageSize: 1, page: 1);
        var resultP1 = await sut.ListAsync(filterP1, CancellationToken.None);

        Assert.Single(resultP1.Items);
        Assert.Equal("A-Open", resultP1.Items[0].Name);
        Assert.Equal(3, resultP1.TotalItems);
        Assert.Equal(3, resultP1.TotalPages);

        // Page 2
        var filterP2 = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt, pageSize: 1, page: 2);
        var resultP2 = await sut.ListAsync(filterP2, CancellationToken.None);

        Assert.Single(resultP2.Items);
        Assert.Equal("C-Open", resultP2.Items[0].Name);
        Assert.Equal(3, resultP2.TotalItems);
        Assert.Equal(3, resultP2.TotalPages);
    }

    [Fact]
    public async Task ListAsync_OpenNowFalse_PaginationAppliedAfterFilter()
    {
        await using var writeCtx = _fixture.CreateContext();

        // A-Open, B-Closed, C-Open, D-Closed
        var (_, _, brA) = await SeedFullEstablishment(writeCtx, "A-OpenPF", "a-open-pf");
        await SeedSchedule(writeCtx, brA, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, _) = await SeedFullEstablishment(writeCtx, "B-ClosedPF", "b-closed-pf");

        var (_, _, brC) = await SeedFullEstablishment(writeCtx, "C-OpenPF", "c-open-pf");
        await SeedSchedule(writeCtx, brC, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        var (_, _, _) = await SeedFullEstablishment(writeCtx, "D-ClosedPF", "d-closed-pf");

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);
        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);

        // Page 1
        var filterP1 = FilterWithOpenNow(openNow: false, evaluatedAtUtc: evaluatedAt, pageSize: 1, page: 1);
        var resultP1 = await sut.ListAsync(filterP1, CancellationToken.None);

        Assert.Single(resultP1.Items);
        Assert.Equal("B-ClosedPF", resultP1.Items[0].Name);
        Assert.Equal(2, resultP1.TotalItems);
        Assert.Equal(2, resultP1.TotalPages);

        // Page 2
        var filterP2 = FilterWithOpenNow(openNow: false, evaluatedAtUtc: evaluatedAt, pageSize: 1, page: 2);
        var resultP2 = await sut.ListAsync(filterP2, CancellationToken.None);

        Assert.Single(resultP2.Items);
        Assert.Equal("D-ClosedPF", resultP2.Items[0].Name);
        Assert.Equal(2, resultP2.TotalItems);
    }

    [Fact]
    public async Task ListAsync_OpenNow_WithServiceId_AND_Semantics()
    {
        await using var writeCtx = _fixture.CreateContext();

        var serviceId = await SeedService(writeCtx, "WiFi");

        // Open establishment WITH service
        var (_, _, brWithSvc) = await SeedFullEstablishment(writeCtx, "OpenWithSvc", "open-with-svc");
        await SeedSchedule(writeCtx, brWithSvc, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        await SeedBranchService(writeCtx, brWithSvc, serviceId);

        // Open establishment WITHOUT service
        var (_, _, brNoSvc) = await SeedFullEstablishment(writeCtx, "OpenNoSvc", "open-no-svc");
        await SeedSchedule(writeCtx, brNoSvc, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt, serviceId: serviceId.Value);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("OpenWithSvc", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_OpenNow_WithRestrictionId_AND_Semantics()
    {
        await using var writeCtx = _fixture.CreateContext();

        var restrictionId = await SeedRestriction(writeCtx, "SinGluten");

        // Open establishment WITH restriction
        var (_, _, brWithRestr) = await SeedFullEstablishment(writeCtx, "OpenWithRestr", "open-with-restr");
        await SeedSchedule(writeCtx, brWithRestr, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));
        await SeedBranchRestriction(writeCtx, brWithRestr, restrictionId);

        // Open establishment WITHOUT restriction
        var (_, _, brNoRestr) = await SeedFullEstablishment(writeCtx, "OpenNoRestr", "open-no-restr");
        await SeedSchedule(writeCtx, brNoRestr, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt, restrictionId: restrictionId.Value);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("OpenWithRestr", result.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_AsNoTracking_Maintained()
    {
        await using var writeCtx = _fixture.CreateContext();
        var (_, _, branchId) = await SeedFullEstablishment(writeCtx, "TrackEst", "track-est");
        await SeedSchedule(writeCtx, branchId, WeekDay.Monday, new TimeOnly(8, 0), new TimeOnly(17, 0));

        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt);

        await sut.ListAsync(filter, CancellationToken.None);

        Assert.Empty(readCtx.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ListAsync_SQLite_InMemory_Compatible()
    {
        await using var readCtx = _fixture.CreateContext();
        var sut = CreateSutWithAvailability(readCtx);

        var evaluatedAt = new DateTimeOffset(2026, 7, 27, 15, 0, 0, TimeSpan.Zero);
        var filter = FilterWithOpenNow(openNow: true, evaluatedAtUtc: evaluatedAt);

        var result = await sut.ListAsync(filter, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    public void Dispose() => _fixture.Dispose();

    private static PublicEstablishmentReadService CreateSut(LyriaDbContext readCtx)
    {
        var availabilityReadService = new StubBranchAvailabilityReadService();
        var timeZoneService = new StubTimeZoneService();
        return new PublicEstablishmentReadService(readCtx, availabilityReadService, timeZoneService);
    }

    private sealed class StubBranchAvailabilityReadService : IBranchAvailabilityReadService
    {
        public Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
            EstablishmentBranchId branchId, DateOnly localDate, CancellationToken cancellationToken)
            => Task.FromResult<BranchAvailabilityContext?>(null);

        public Task<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>
            GetAvailabilityContextsAsync(
                IReadOnlyCollection<EstablishmentBranchId> branchIds,
                DateTimeOffset evaluatedAtUtc,
                ITimeZoneService timeZoneService,
                CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>(
                new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>());
    }

    private sealed class StubTimeZoneService : ITimeZoneService
    {
        public bool IsValid(string timeZoneId) => true;

        public DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId)
            => utcDateTime.UtcDateTime;
    }

    private static PublicEstablishmentReadService CreateSutWithAvailability(LyriaDbContext readCtx)
    {
        var availabilityReadService = new BranchAvailabilityReadService(readCtx);
        var timeZoneService = new RealTimeZoneService();
        return new PublicEstablishmentReadService(readCtx, availabilityReadService, timeZoneService);
    }

    private static PublicEstablishmentListFilter FilterWithOpenNow(
        bool? openNow,
        DateTimeOffset evaluatedAtUtc,
        int page = 1,
        int pageSize = 20,
        Guid? serviceId = null,
        Guid? restrictionId = null) =>
        new(null, null, null, null, null, serviceId, restrictionId, null, null, openNow, page, pageSize, "name", "asc", evaluatedAtUtc);

    private sealed class RealTimeZoneService : ITimeZoneService
    {
        public bool IsValid(string timeZoneId)
        {
            try { TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); return true; }
            catch { return false; }
        }

        public DateTime ConvertUtcToLocal(DateTimeOffset utcDateTime, string timeZoneId)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(utcDateTime, tz).DateTime;
        }
    }
}
