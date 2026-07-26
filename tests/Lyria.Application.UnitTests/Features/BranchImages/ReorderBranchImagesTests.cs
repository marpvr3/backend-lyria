using Lyria.Application.Features.BranchImages.Reorder;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class ReorderBranchImagesTests
{
    private readonly FakeBranchImageRepository _imageRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly ReorderBranchImagesCommandHandler _handler;

    public ReorderBranchImagesTests()
    {
        _handler = new ReorderBranchImagesCommandHandler(
            _imageRepository, _branchRepository);
    }

    private (Guid branchId, BranchImage img1, BranchImage img2) SeedBranchAndImages()
    {
        var branchId = Guid.NewGuid();
        var branchIdTyped = new EstablishmentBranchId(branchId);

        var branch = EstablishmentBranch.Create(
            branchIdTyped, EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);

        var img1 = BranchImage.Create(
            BranchImageId.New(), branchIdTyped,
            "https://cdn.example.com/a.jpg", "a.jpg",
            null, false, 0);
        _imageRepository.Seed(img1);

        var img2 = BranchImage.Create(
            BranchImageId.New(), branchIdTyped,
            "https://cdn.example.com/b.jpg", "b.jpg",
            null, false, 1);
        _imageRepository.Seed(img2);

        return (branchId, img1, img2);
    }

    [Fact]
    public async Task Handle_ValidReorder_ReturnsSuccess()
    {
        var (branchId, img1, img2) = SeedBranchAndImages();

        var command = new ReorderBranchImagesCommand(branchId,
        [
            new ImageOrderItem(img1.Id.Value, 1),
            new ImageOrderItem(img2.Id.Value, 0)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, img1.SortOrder);
        Assert.Equal(0, img2.SortOrder);
    }

    [Fact]
    public async Task Handle_DuplicateIds_ReturnsValidation()
    {
        var (branchId, img1, _) = SeedBranchAndImages();

        var command = new ReorderBranchImagesCommand(branchId,
        [
            new ImageOrderItem(img1.Id.Value, 0),
            new ImageOrderItem(img1.Id.Value, 1)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.DuplicateImageOrderEntry", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NegativeOrder_ReturnsValidation()
    {
        var (branchId, img1, _) = SeedBranchAndImages();

        var command = new ReorderBranchImagesCommand(branchId,
        [
            new ImageOrderItem(img1.Id.Value, -1)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.InvalidSortOrder", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ImageFromDifferentBranch_ReturnsNotFound()
    {
        var (branchId, _, _) = SeedBranchAndImages();

        var command = new ReorderBranchImagesCommand(branchId,
        [
            new ImageOrderItem(Guid.NewGuid(), 0)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new ReorderBranchImagesCommand(Guid.NewGuid(),
        [
            new ImageOrderItem(Guid.NewGuid(), 0)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_FailureDoesNotPartiallyUpdate()
    {
        var (branchId, img1, _) = SeedBranchAndImages();
        int originalOrder = img1.SortOrder;

        var command = new ReorderBranchImagesCommand(branchId,
        [
            new ImageOrderItem(img1.Id.Value, 5),
            new ImageOrderItem(Guid.NewGuid(), 0)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(originalOrder, img1.SortOrder);
        Assert.Equal(0, _imageRepository.SaveChangesCallCount);
    }
}
