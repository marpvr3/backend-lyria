using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicEstablishmentBySlugTests
{
    private readonly FakePublicEstablishmentReadService _readService = new();
    private readonly GetPublicEstablishmentBySlugQueryHandler _handler;

    private static readonly PublicEstablishmentDetailResponse SeedDetail = new(
        Guid.NewGuid(),
        "Alpha Bistro",
        "alpha-bistro",
        "Cocina vegana",
        "https://alpha.com",
        "@alphabistro",
        null,
        "contacto@alpha.com",
        "+57123456789",
        new PublicCategoryDetailResponse(Guid.NewGuid(), "Restaurante", null, null),
        []);

    public GetPublicEstablishmentBySlugTests()
    {
        _readService.SeedDetail("alpha-bistro", SeedDetail);
        _handler = new GetPublicEstablishmentBySlugQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsEstablishmentBySlug()
    {
        var query = new GetPublicEstablishmentBySlugQuery("alpha-bistro");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Bistro", result.Value.Name);
        Assert.Equal("alpha-bistro", result.Value.Slug);
    }

    [Fact]
    public async Task Handle_NormalizesSlug()
    {
        // Establishment.NormalizeSlug trims and lowercases
        var query = new GetPublicEstablishmentBySlugQuery("  Alpha-Bistro  ");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Bistro", result.Value.Name);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenNotExists()
    {
        var query = new GetPublicEstablishmentBySlugQuery("non-existent-slug");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("PublicCatalog.EstablishmentNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenInactive()
    {
        // An inactive establishment would not be seeded in the read service
        // (the infrastructure layer filters them out), so querying returns null => NotFound
        var query = new GetPublicEstablishmentBySlugQuery("inactive-establishment");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenCategoryInactive()
    {
        // An establishment with an inactive category would not be returned by the read service
        var query = new GetPublicEstablishmentBySlugQuery("category-inactive-slug");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
