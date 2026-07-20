using Lyria.Application.Features.EstablishmentCategories.Deactivate;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentCategories;

public sealed class DeactivateEstablishmentCategoryTests
{
    private readonly FakeEstablishmentCategoryRepository _repository = new();
    private readonly DeactivateEstablishmentCategoryCommandHandler _handler;

    public DeactivateEstablishmentCategoryTests()
    {
        _handler = new DeactivateEstablishmentCategoryCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new DeactivateEstablishmentCategoryCommand(Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_DeactivatesCategory()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        _repository.Seed(category);

        var command = new DeactivateEstablishmentCategoryCommand(category.Id.Value);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AlreadyInactive_IsIdempotent()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        category.Deactivate();
        _repository.Seed(category);

        var command = new DeactivateEstablishmentCategoryCommand(category.Id.Value);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsActive);
    }
}
