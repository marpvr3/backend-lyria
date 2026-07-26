using Lyria.Application.Features.BranchImages.SetPrimary;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class SetBranchImagePrimaryTests
{
    private readonly FakeBranchImageRepository _imageRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly SetBranchImagePrimaryCommandHandler _handler;

    public SetBranchImagePrimaryTests()
    {
        _handler = new SetBranchImagePrimaryCommandHandler(
            _imageRepository, _branchRepository);
    }

    private (Guid branchId, BranchImage image) SeedBranchAndImage(bool isPrimary = false)
    {
        var branchId = Guid.NewGuid();
        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(branchId),
            EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);

        var image = BranchImage.Create(
            BranchImageId.New(),
            new EstablishmentBranchId(branchId),
            "https://cdn.example.com/img.jpg", "img.jpg",
            null, isPrimary, 0);
        _imageRepository.Seed(image);

        return (branchId, image);
    }

    [Fact]
    public async Task Handle_SetPrimary_ReturnsSuccess()
    {
        var (branchId, image) = SeedBranchAndImage();

        var command = new SetBranchImagePrimaryCommand(branchId, image.Id.Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(image.IsPrimary);
    }

    [Fact]
    public async Task Handle_AlreadyPrimary_Idempotent()
    {
        var (branchId, image) = SeedBranchAndImage(isPrimary: true);

        var command = new SetBranchImagePrimaryCommand(branchId, image.Id.Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(image.IsPrimary);
    }

    [Fact]
    public async Task Handle_UnsetsPreviousPrimary()
    {
        var branchId = Guid.NewGuid();
        var branchIdTyped = new EstablishmentBranchId(branchId);
        var branch = EstablishmentBranch.Create(
            branchIdTyped, EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);

        var primaryImage = BranchImage.Create(
            BranchImageId.New(), branchIdTyped,
            "https://cdn.example.com/old.jpg", "old.jpg",
            null, true, 0);
        _imageRepository.Seed(primaryImage);

        var newImage = BranchImage.Create(
            BranchImageId.New(), branchIdTyped,
            "https://cdn.example.com/new.jpg", "new.jpg",
            null, false, 1);
        _imageRepository.Seed(newImage);

        var command = new SetBranchImagePrimaryCommand(branchId, newImage.Id.Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(primaryImage.IsPrimary);
        Assert.True(newImage.IsPrimary);
    }

    [Fact]
    public async Task Handle_InactiveImage_ReturnsConflict()
    {
        var (branchId, image) = SeedBranchAndImage();
        image.Deactivate();

        var command = new SetBranchImagePrimaryCommand(branchId, image.Id.Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new SetBranchImagePrimaryCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
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
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);

        var command = new SetBranchImagePrimaryCommand(branchId, Guid.NewGuid());

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
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(otherBranch);

        var command = new SetBranchImagePrimaryCommand(otherBranchId, image.Id.Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.NotFound", result.Error.Code);
    }
}
