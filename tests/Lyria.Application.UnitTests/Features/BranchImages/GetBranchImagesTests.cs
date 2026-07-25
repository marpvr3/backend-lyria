using Lyria.Application.Features.BranchImages;
using Lyria.Application.Features.BranchImages.GetByBranch;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class GetBranchImagesTests
{
    private readonly FakeBranchImageReadService _readService = new();
    private readonly GetBranchImagesQueryHandler _handler;

    public GetBranchImagesTests()
    {
        _handler = new GetBranchImagesQueryHandler(_readService);
    }

    [Fact]
    public async Task Handle_ValidBranch_ReturnsImages()
    {
        var branchId = Guid.NewGuid();
        var branchIdTyped = new EstablishmentBranchId(branchId);
        _readService.SeedBranch(branchIdTyped);

        var images = new List<BranchImageResponse>
        {
            new(Guid.NewGuid(), "https://cdn.example.com/img.jpg", "img.jpg",
                "Alt", true, 0, true)
        };
        _readService.SetResponse(new BranchImagesResponse(branchId, images));

        var query = new GetBranchImagesQuery(branchId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Images);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var query = new GetBranchImagesQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }
}
