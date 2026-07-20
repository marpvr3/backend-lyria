using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.EstablishmentCategories.ListActive;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentCategories;

public sealed class ListActiveEstablishmentCategoriesTests
{
    private readonly FakeEstablishmentCategoryReadService _readService = new();
    private readonly ListActiveEstablishmentCategoriesQueryHandler _handler;

    public ListActiveEstablishmentCategoriesTests()
    {
        _handler = new ListActiveEstablishmentCategoriesQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveCategories()
    {
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Cafetería", null, null, 0, true));
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Bar", null, null, 1, false));
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Restaurante", null, null, 2, true));

        var query = new ListActiveEstablishmentCategoriesQuery();

        IReadOnlyList<EstablishmentCategoryResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsActive));
    }

    [Fact]
    public async Task Handle_ReturnsOrderedBySortOrderThenName()
    {
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Cafetería", null, null, 2, true));
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Bar", null, null, 1, true));
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Almacén", null, null, 1, true));

        var query = new ListActiveEstablishmentCategoriesQuery();

        IReadOnlyList<EstablishmentCategoryResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("Almacén", result[0].Name);
        Assert.Equal("Bar", result[1].Name);
        Assert.Equal("Cafetería", result[2].Name);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyCollection_WhenNoActive()
    {
        _readService.Seed(new EstablishmentCategoryResponse(
            Guid.NewGuid(), "Bar", null, null, 0, false));

        var query = new ListActiveEstablishmentCategoriesQuery();

        IReadOnlyList<EstablishmentCategoryResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyCollection_WhenNoneExist()
    {
        var query = new ListActiveEstablishmentCategoriesQuery();

        IReadOnlyList<EstablishmentCategoryResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }
}
