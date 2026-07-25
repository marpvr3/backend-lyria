using Lyria.Application.Common;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetEstablishments;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicEstablishmentsTests
{
    private static readonly Guid CategoryRestaurantId = Guid.NewGuid();
    private static readonly Guid CategoryCafeId = Guid.NewGuid();
    private static readonly Guid ServiceVeganId = Guid.NewGuid();
    private static readonly Guid ServiceGlutenFreeId = Guid.NewGuid();
    private static readonly Guid RestrictionPetsId = Guid.NewGuid();
    private static readonly Guid RestrictionWheelchairId = Guid.NewGuid();

    private static readonly PublicCategoryBriefResponse CategoryRestaurant =
        new(CategoryRestaurantId, "Restaurante");

    private static readonly PublicCategoryBriefResponse CategoryCafe =
        new(CategoryCafeId, "Cafetería");

    private readonly FakePublicEstablishmentReadService _readService = new();
    private readonly GetPublicEstablishmentsQueryHandler _handler;

    public GetPublicEstablishmentsTests()
    {
        // Establishment 1: "Alpha Bistro" — Restaurant in Bogotá, Colombia
        // Branch 1: has ServiceVegan + RestrictionPets (complianceLevel 1, certified)
        _readService.Seed(new FakePublicEstablishmentSeed(
            Guid.NewGuid(),
            "Alpha Bistro",
            "alpha-bistro",
            "Cocina vegana y orgánica",
            null,
            null,
            CategoryRestaurant,
            [
                new FakePublicBranchSeed(
                    Guid.NewGuid(), "Sede Norte", "Bogotá", "Cundinamarca", "Colombia",
                    [new FakePublicBranchServiceSeed(ServiceVeganId, "Vegano")],
                    [new FakePublicBranchRestrictionSeed(RestrictionPetsId, "Mascotas", 1, true)])
            ],
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        // Establishment 2: "Beta Café" — Cafe in Medellín, Colombia
        // Branch 1: has ServiceGlutenFree + RestrictionWheelchair (complianceLevel 2, not certified)
        _readService.Seed(new FakePublicEstablishmentSeed(
            Guid.NewGuid(),
            "Beta Café",
            "beta-cafe",
            "Café artesanal con opciones sin gluten",
            null,
            null,
            CategoryCafe,
            [
                new FakePublicBranchSeed(
                    Guid.NewGuid(), "Centro", "Medellín", "Antioquia", "Colombia",
                    [new FakePublicBranchServiceSeed(ServiceGlutenFreeId, "Sin Gluten")],
                    [new FakePublicBranchRestrictionSeed(RestrictionWheelchairId, "Silla de ruedas", 2, false)])
            ],
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        // Establishment 3: "Gamma Sushi" — Restaurant in Lima, Perú
        // Branch 1: has ServiceVegan (no restriction)
        // Branch 2: has RestrictionPets (complianceLevel 3, certified) — different branch
        _readService.Seed(new FakePublicEstablishmentSeed(
            Guid.NewGuid(),
            "Gamma Sushi",
            "gamma-sushi",
            "Sushi fresco",
            null,
            null,
            CategoryRestaurant,
            [
                new FakePublicBranchSeed(
                    Guid.NewGuid(), "Miraflores", "Lima", "Lima Metropolitana", "Perú",
                    [new FakePublicBranchServiceSeed(ServiceVeganId, "Vegano")],
                    []),
                new FakePublicBranchSeed(
                    Guid.NewGuid(), "San Isidro", "Lima", "Lima Metropolitana", "Perú",
                    [],
                    [new FakePublicBranchRestrictionSeed(RestrictionPetsId, "Mascotas", 3, true)])
            ],
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        _handler = new GetPublicEstablishmentsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsFirstPage()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_AppliesPageAndPageSize()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 2, 1);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task Handle_SearchesByName()
    {
        var query = new GetPublicEstablishmentsQuery(
            "Alpha", null, null, null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Alpha Bistro", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_SearchesByDescription()
    {
        var query = new GetPublicEstablishmentsQuery(
            "artesanal", null, null, null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Beta Café", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByCategory()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, CategoryCafeId, null, null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Beta Café", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByCity()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, "Bogotá", null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Alpha Bistro", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByProvince()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, "Antioquia", null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Beta Café", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByCountry()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, "Perú", null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Gamma Sushi", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByService()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, ServiceVeganId, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
    }

    [Fact]
    public async Task Handle_FiltersByRestriction()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, RestrictionWheelchairId, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Beta Café", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByComplianceLevel()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, 1, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Alpha Bistro", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_FiltersByIsCertified()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, false, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Beta Café", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_CombinesServiceAndRestrictionInSameBranch()
    {
        // Alpha Bistro has ServiceVegan + RestrictionPets in the SAME branch
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, ServiceVeganId, RestrictionPetsId, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Alpha Bistro", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_DoesNotMatchWhenFiltersAreInDifferentBranches()
    {
        // Gamma Sushi has ServiceVegan in branch 1 and RestrictionPets in branch 2
        // Filtering by both should NOT return Gamma Sushi (same-branch semantics)
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, ServiceVeganId, RestrictionPetsId, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Only Alpha Bistro matches; Gamma Sushi should be excluded
        Assert.DoesNotContain(result.Value.Items, i => i.Name == "Gamma Sushi");
    }

    [Fact]
    public async Task Handle_CombinesMultipleFiltersWithAnd()
    {
        // Category=Restaurant AND Service=Vegan => Alpha Bistro + Gamma Sushi
        var query = new GetPublicEstablishmentsQuery(
            null, CategoryRestaurantId, null, null, null, ServiceVeganId, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.All(result.Value.Items, i => Assert.Equal(CategoryRestaurantId, i.Category.Id));
    }

    [Fact]
    public async Task Handle_SortsByNameAsc()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "name", "asc");

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Bistro", result.Value.Items[0].Name);
        Assert.Equal("Beta Café", result.Value.Items[1].Name);
        Assert.Equal("Gamma Sushi", result.Value.Items[2].Name);
    }

    [Fact]
    public async Task Handle_SortsByNameDesc()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "name", "desc");

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Gamma Sushi", result.Value.Items[0].Name);
        Assert.Equal("Beta Café", result.Value.Items[1].Name);
        Assert.Equal("Alpha Bistro", result.Value.Items[2].Name);
    }

    [Fact]
    public async Task Handle_SortsByNewest()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "newest", "desc");

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Gamma Sushi", result.Value.Items[0].Name);
        Assert.Equal("Beta Café", result.Value.Items[1].Name);
        Assert.Equal("Alpha Bistro", result.Value.Items[2].Name);
    }

    [Fact]
    public async Task Handle_SortsByBranchCountDesc()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20, "branchCount", "desc");

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Gamma Sushi has 2 branches, Alpha and Beta have 1 each
        Assert.Equal("Gamma Sushi", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListCorrectly()
    {
        var emptyService = new FakePublicEstablishmentReadService();
        var handler = new GetPublicEstablishmentsQueryHandler(emptyService);

        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 20);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task Handle_CalculatesPaginationMetadata()
    {
        var query = new GetPublicEstablishmentsQuery(
            null, null, null, null, null, null, null, null, null, 1, 2);

        Result<PagedResponse<PublicEstablishmentListItemResponse>> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.Equal(2, result.Value.TotalPages);
    }
}
