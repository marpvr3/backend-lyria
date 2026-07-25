using Lyria.Application.Features.BranchImages.UpdateStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class UpdateBranchImageStatusTests
{
    private readonly FakeBranchImageRepository _imageRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly UpdateBranchImageStatusCommandHandler _handler;

    public UpdateBranchImageStatusTests()
    {
        _handler = new UpdateBranchImageStatusCommandHandler(
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
            null, null, null, null, null);
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
    public async Task Handle_Deactivate_ReturnsSuccess()
    {
        var (branchId, image) = SeedBranchAndImage();

        var command = new UpdateBranchImageStatusCommand(branchId, image.Id.Value, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(image.IsActive);
    }

    [Fact]
    public async Task Handle_DeactivatePrimary_UnsetsPrimary()
    {
        var (branchId, image) = SeedBranchAndImage(isPrimary: true);

        var command = new UpdateBranchImageStatusCommand(branchId, image.Id.Value, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(image.IsActive);
        Assert.False(image.IsPrimary);
    }

    [Fact]
    public async Task Handle_Activate_ReturnsSuccess()
    {
        var (branchId, image) = SeedBranchAndImage();
        image.Deactivate();

        var command = new UpdateBranchImageStatusCommand(branchId, image.Id.Value, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(image.IsActive);
    }

    [Fact]
    public async Task Handle_AlreadyActive_Idempotent()
    {
        var (branchId, image) = SeedBranchAndImage();

        var command = new UpdateBranchImageStatusCommand(branchId, image.Id.Value, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(image.IsActive);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchImageStatusCommand(Guid.NewGuid(), Guid.NewGuid(), false);

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
            null, null, null, null, null);
        _branchRepository.Seed(branch);

        var command = new UpdateBranchImageStatusCommand(branchId, Guid.NewGuid(), false);

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

        var command = new UpdateBranchImageStatusCommand(otherBranchId, image.Id.Value, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BranchImage.NotFound", result.Error.Code);
    }
}
