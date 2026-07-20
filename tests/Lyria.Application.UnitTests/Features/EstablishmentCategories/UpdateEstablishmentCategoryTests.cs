using Lyria.Application.Features.EstablishmentCategories.Update;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentCategories;

public sealed class UpdateEstablishmentCategoryTests
{
    private readonly FakeEstablishmentCategoryRepository _repository = new();
    private readonly UpdateEstablishmentCategoryCommandHandler _handler;

    public UpdateEstablishmentCategoryTests()
    {
        _handler = new UpdateEstablishmentCategoryCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new UpdateEstablishmentCategoryCommand(Guid.NewGuid(), "Café", null, null, 0);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNameDuplicate_ReturnsConflict()
    {
        var existing = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 0);
        var toUpdate = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        _repository.Seed(existing);
        _repository.Seed(toUpdate);

        var command = new UpdateEstablishmentCategoryCommand(
            toUpdate.Id.Value, "Restaurante", null, null, 0);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_AllowsSameNameOnSameEntity()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        _repository.Seed(category);

        var command = new UpdateEstablishmentCategoryCommand(
            category.Id.Value, "Cafetería", "Nueva descripción", null, 5);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UpdatesAndSaves()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        _repository.Seed(category);

        var command = new UpdateEstablishmentCategoryCommand(
            category.Id.Value, "Bar", "Un bar", null, 3);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bar", category.Name);
        Assert.Equal("Un bar", category.Description);
        Assert.Equal(3, category.SortOrder);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNotFound_DoesNotSave()
    {
        var command = new UpdateEstablishmentCategoryCommand(Guid.NewGuid(), "Café", null, null, 0);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
