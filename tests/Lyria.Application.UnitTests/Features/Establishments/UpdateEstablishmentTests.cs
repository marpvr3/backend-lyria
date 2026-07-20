using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Establishments.Update;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Establishments;

public sealed class UpdateEstablishmentTests
{
    private static readonly Guid ActiveCategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AnotherCategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly FakeEstablishmentRepository _repository = new();
    private readonly FakeEstablishmentCategoryReadService _categoryReadService = new();
    private readonly UpdateEstablishmentCommandHandler _handler;

    public UpdateEstablishmentTests()
    {
        _categoryReadService.Seed(new EstablishmentCategoryResponse(
            ActiveCategoryId, "Restaurante", null, null, 1, true));
        _categoryReadService.Seed(new EstablishmentCategoryResponse(
            AnotherCategoryId, "Cafetería", null, null, 2, true));
        _handler = new UpdateEstablishmentCommandHandler(_repository, _categoryReadService, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new UpdateEstablishmentCommand(
            Guid.NewGuid(), ActiveCategoryId, "Test", "test", null, null, null, null, null, null, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNotFound_DoesNotSave()
    {
        var command = new UpdateEstablishmentCommand(
            Guid.NewGuid(), ActiveCategoryId, "Test", "test", null, null, null, null, null, null, false);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_CategoryNotAvailable_ReturnsValidation()
    {
        var establishment = CreateAndSeedEstablishment();
        var command = new UpdateEstablishmentCommand(
            establishment.Id.Value, Guid.NewGuid(), "Test", "test", null, null, null, null, null, null, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("CategoryNotAvailable", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SlugDuplicate_ReturnsConflict()
    {
        var first = CreateAndSeedEstablishment("first-slug");
        var second = CreateAndSeedEstablishment("second-slug");

        var command = new UpdateEstablishmentCommand(
            second.Id.Value, ActiveCategoryId, "Updated", "first-slug", null, null, null, null, null, null, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_AllowsSameSlugOnSameEntity()
    {
        var establishment = CreateAndSeedEstablishment("my-slug");

        var command = new UpdateEstablishmentCommand(
            establishment.Id.Value, ActiveCategoryId, "Updated Name", "my-slug",
            null, null, null, null, null, null, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidInput_UpdatesAndSaves()
    {
        var establishment = CreateAndSeedEstablishment();

        var command = new UpdateEstablishmentCommand(
            establishment.Id.Value, AnotherCategoryId, "Updated", "updated-slug",
            "New desc", "https://new.com", "@new", null, null, null, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", establishment.Name);
        Assert.Equal("updated-slug", establishment.Slug);
        Assert.Equal("New desc", establishment.Description);
        Assert.Equal("https://new.com", establishment.Website);
        Assert.Equal("@new", establishment.Instagram);
        Assert.Equal(AnotherCategoryId, establishment.CategoryId.Value);
        Assert.True(establishment.IsVerified);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_IsVerifiedFalse_Unverifies()
    {
        var establishment = CreateAndSeedEstablishment();
        establishment.Verify(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

        var command = new UpdateEstablishmentCommand(
            establishment.Id.Value, ActiveCategoryId, "Test", "test-slug",
            null, null, null, null, null, null, false);

        await _handler.Handle(command, CancellationToken.None);

        Assert.False(establishment.IsVerified);
    }

    private Establishment CreateAndSeedEstablishment(string slug = "test-slug")
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(),
            new EstablishmentCategoryId(ActiveCategoryId),
            "Test Place",
            slug,
            null,
            null,
            null,
            null,
            null,
            null);
        _repository.Seed(establishment);
        return establishment;
    }
}
