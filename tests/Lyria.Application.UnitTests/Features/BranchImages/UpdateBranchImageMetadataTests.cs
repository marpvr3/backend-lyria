using Lyria.Application.Features.BranchImages.UpdateMetadata;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class UpdateBranchImageMetadataTests
{
    private readonly FakeBranchImageRepository _imageRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly UpdateBranchImageMetadataCommandHandler _handler;

    public UpdateBranchImageMetadataTests()
    {
        _handler = new UpdateBranchImageMetadataCommandHandler(
            _imageRepository, _branchRepository);
    }

    private (Guid branchId, BranchImage image) SeedBranchAndImage()
    {
        var branchId = Guid.NewGuid();
        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(branchId),
            EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        _branchRepository.Seed(branch);

        var image = BranchImage.Create(
            BranchImageId.New(),
            new EstablishmentBranchId(branchId),
            "https://cdn.example.com/old.jpg", "old.jpg",
            "Old text", false, 0);
        _imageRepository.Seed(image);

        return (branchId, image);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsSuccess()
    {
        var (branchId, image) = SeedBranchAndImage();

        var command = new UpdateBranchImageMetadataCommand(
            branchId, image.Id.Value,
            "https://cdn.example.com/new.jpg", "new.jpg",
            "New text", 5);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.example.com/new.jpg", image.Url);
        Assert.Equal("new.jpg", image.FileName);
        Assert.Equal("New text", image.AlternativeText);
        Assert.Equal(5, image.SortOrder);
    }

    [Fact]
    public async Task Handle_ImageNotFound_ReturnsNotFound()
    {
        var branchId = Guid.NewGuid();
        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(branchId),
            EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        _branchRepository.Seed(branch);

        var command = new UpdateBranchImageMetadataCommand(
            branchId, Guid.NewGuid(),
            "https://cdn.example.com/new.jpg", "new.jpg",
            null, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ImageFromDifferentBranch_ReturnsNotFound()
    {
        var (_, image) = SeedBranchAndImage();

        var otherBranchId = Guid.NewGuid();
        var otherBranch = EstablishmentBranch.Create(
            new EstablishmentBranchId(otherBranchId),
            EstablishmentId.New(),
            "Otra Sede", "Otra Calle 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        _branchRepository.Seed(otherBranch);

        var command = new UpdateBranchImageMetadataCommand(
            otherBranchId, image.Id.Value,
            "https://cdn.example.com/new.jpg", "new.jpg",
            null, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchImageMetadataCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            "https://cdn.example.com/new.jpg", "new.jpg",
            null, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }
}
