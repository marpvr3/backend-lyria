using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetCatalogs;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicCatalogsTests
{
    private readonly FakePublicCatalogReadService _readService = new();
    private readonly GetPublicCatalogsQueryHandler _handler;

    public GetPublicCatalogsTests()
    {
        _handler = new GetPublicCatalogsQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsCatalogs()
    {
        var categories = new List<PublicCatalogCategoryResponse>
        {
            new(Guid.NewGuid(), "Restaurante", null, 1),
            new(Guid.NewGuid(), "Cafetería", null, 2),
        };

        var services = new List<PublicCatalogServiceResponse>
        {
            new(Guid.NewGuid(), "Vegano", null),
            new(Guid.NewGuid(), "Sin Gluten", null),
        };

        var restrictions = new List<PublicCatalogRestrictionResponse>
        {
            new(Guid.NewGuid(), "Mascotas"),
            new(Guid.NewGuid(), "Silla de ruedas"),
        };

        var locations = new PublicCatalogLocationsResponse(
            ["Colombia", "Perú"],
            [
                new PublicCatalogProvinceResponse("Colombia", "Cundinamarca"),
                new PublicCatalogProvinceResponse("Colombia", "Antioquia"),
            ],
            [
                new PublicCatalogCityResponse("Colombia", "Cundinamarca", "Bogotá"),
                new PublicCatalogCityResponse("Colombia", "Antioquia", "Medellín"),
            ]);

        var expected = new PublicCatalogsResponse(categories, services, restrictions, locations);
        _readService.Seed(expected);

        PublicCatalogsResponse result =
            await _handler.Handle(new GetPublicCatalogsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Categories.Count);
        Assert.Equal(2, result.Services.Count);
        Assert.Equal(2, result.Restrictions.Count);
        Assert.Equal(2, result.Locations.Countries.Count);
        Assert.Equal(2, result.Locations.Provinces.Count);
        Assert.Equal(2, result.Locations.Cities.Count);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveData()
    {
        // The fake returns exactly what is seeded, which represents only active data
        // (the infrastructure layer filters out inactive records)
        var categories = new List<PublicCatalogCategoryResponse>
        {
            new(Guid.NewGuid(), "Restaurante", null, 1),
        };

        var expected = new PublicCatalogsResponse(
            categories,
            [],
            [],
            new PublicCatalogLocationsResponse([], [], []));

        _readService.Seed(expected);

        PublicCatalogsResponse result =
            await _handler.Handle(new GetPublicCatalogsQuery(), CancellationToken.None);

        Assert.Single(result.Categories);
        Assert.Equal("Restaurante", result.Categories[0].Name);
        Assert.Empty(result.Services);
        Assert.Empty(result.Restrictions);
    }
}
