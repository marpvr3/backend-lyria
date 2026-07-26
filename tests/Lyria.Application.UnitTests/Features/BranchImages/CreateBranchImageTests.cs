using Lyria.Application.Features.BranchImages.Create;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchImages;

public sealed class CreateBranchImageTests
{
    private readonly FakeBranchImageRepository _imageRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly CreateBranchImageCommandHandler _handler;

    public CreateBranchImageTests()
    {
        _handler = new CreateBranchImageCommandHandler(
            _imageRepository, _branchRepository);
    }

    private void SeedActiveBranch(Guid branchId)
    {
        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(branchId),
            EstablishmentId.New(),
            "Sede Test", "Calle Test 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _branchRepository.Seed(branch);
    }

    [Fact]
    public async Task Handle_ValidImage_ReturnsSuccess()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new CreateBranchImageCommand(
            branchId, "https://cdn.example.com/img.jpg", "img.jpg",
            "Alt text", false, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _imageRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new CreateBranchImageCommand(
            Guid.NewGuid(), "https://cdn.example.com/img.jpg", "img.jpg",
            null, false, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_FirstPrimaryImage_ReturnsSuccess()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var command = new CreateBranchImageCommand(
            branchId, "https://cdn.example.com/img.jpg", "img.jpg",
            null, true, 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NewPrimary_UnsetsPreviousPrimary()
    {
        var branchId = Guid.NewGuid();
        SeedActiveBranch(branchId);

        var existingImage = BranchImage.Create(
            BranchImageId.New(),
            new EstablishmentBranchId(branchId),
            "https://cdn.example.com/old.jpg", "old.jpg",
            null, true, 0);
        _imageRepository.Seed(existingImage);

        var command = new CreateBranchImageCommand(
            branchId, "https://cdn.example.com/new.jpg", "new.jpg",
            null, true, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(existingImage.IsPrimary);
    }
}
