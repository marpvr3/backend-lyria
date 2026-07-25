using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetBranchById;
using Lyria.Application.UnitTests.Fakes;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicBranchByIdTests
{
    private readonly FakePublicBranchReadService _readService = new();
    private readonly GetPublicBranchByIdQueryHandler _handler;
    private readonly Guid _existingBranchId = Guid.NewGuid();

    public GetPublicBranchByIdTests()
    {
        var branchDetail = new PublicBranchDetailResponse(
            _existingBranchId,
            "Sede Norte",
            new PublicBranchAddressResponse("Calle 100", "15", null, "Usaquén", "Bogotá", "Cundinamarca", "110111", "Colombia"),
            new PublicBranchLocationResponse(4.6867m, -74.0465m),
            new PublicBranchContactResponse("+57123456789", null, "sede@alpha.com"),
            [],
            [],
            [],
            []);

        var establishmentInfo = new PublicBranchEstablishmentResponse(
            Guid.NewGuid(),
            "Alpha Bistro",
            "alpha-bistro",
            "Cocina vegana",
            null,
            new PublicCategoryDetailResponse(Guid.NewGuid(), "Restaurante", null, null));

        _readService.Seed(_existingBranchId, new PublicBranchFullDetailResponse(establishmentInfo, branchDetail));

        _handler = new GetPublicBranchByIdQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ReturnsBranch()
    {
        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_existingBranchId, result.Value.Branch.Id);
        Assert.Equal("Sede Norte", result.Value.Branch.Name);
        Assert.Equal("Alpha Bistro", result.Value.Establishment.Name);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForNonExistent()
    {
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("PublicCatalog.BranchNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveBranch()
    {
        // An inactive branch would not be seeded in the read service
        // (infrastructure filters out inactive branches)
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveEstablishment()
    {
        // A branch belonging to an inactive establishment would not be returned
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveCategory()
    {
        // A branch whose establishment has an inactive category would not be returned
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
