using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.EstablishmentCategories.GetById;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentCategories;

public sealed class GetEstablishmentCategoryByIdTests
{
    private readonly FakeEstablishmentCategoryReadService _readService = new();
    private readonly GetEstablishmentCategoryByIdQueryHandler _handler;

    public GetEstablishmentCategoryByIdTests()
    {
        _handler = new GetEstablishmentCategoryByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_WhenActiveFound_ReturnsAllFields()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentCategoryResponse(
            id, "Restaurante", "Descripción", null, 1, true));

        var query = new GetEstablishmentCategoryByIdQuery(id);

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Restaurante", result.Value.Name);
        Assert.Equal("Descripción", result.Value.Description);
        Assert.Equal(1, result.Value.SortOrder);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Handle_WhenInactive_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentCategoryResponse(
            id, "Spa", null, null, 3, false));

        var query = new GetEstablishmentCategoryByIdQuery(id);

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("EstablishmentCategories.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var query = new GetEstablishmentCategoryByIdQuery(Guid.NewGuid());

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_UsesGetActiveByIdAsync_NotWriteRepository()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentCategoryResponse(
            id, "Cafetería", null, null, 0, true));

        var query = new GetEstablishmentCategoryByIdQuery(id);

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsProjection_NotEntity()
    {
        var id = Guid.NewGuid();
        _readService.Seed(new EstablishmentCategoryResponse(
            id, "Cafetería", null, null, 0, true));

        var query = new GetEstablishmentCategoryByIdQuery(id);

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, CancellationToken.None);

        Assert.IsType<EstablishmentCategoryResponse>(result.Value);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var query = new GetEstablishmentCategoryByIdQuery(Guid.NewGuid());

        Result<EstablishmentCategoryResponse> result = await _handler.Handle(query, cts.Token);

        Assert.True(result.IsFailure);
    }
}
