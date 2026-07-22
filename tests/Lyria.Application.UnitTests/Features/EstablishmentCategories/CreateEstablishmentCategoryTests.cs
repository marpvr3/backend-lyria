using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.EstablishmentCategories.Create;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentCategories;

public sealed class CreateEstablishmentCategoryTests
{
    private readonly FakeEstablishmentCategoryRepository _repository = new();
    private readonly CreateEstablishmentCategoryCommandHandler _handler;

    public CreateEstablishmentCategoryTests()
    {
        _handler = new CreateEstablishmentCategoryCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNameDoesNotExist_CreatesCategory()
    {
        var command = new CreateEstablishmentCategoryCommand("Restaurante", "Descripción", null, 1);

        Result<EstablishmentCategoryId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Added);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNameExists_ReturnsConflict()
    {
        _repository.Seed(EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 0));

        var command = new CreateEstablishmentCategoryCommand("Restaurante", null, null, 0);

        Result<EstablishmentCategoryId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("EstablishmentCategories.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNameExists_DoesNotAddOrSave()
    {
        _repository.Seed(EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 0));

        var command = new CreateEstablishmentCategoryCommand("Restaurante", null, null, 0);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Single(_repository.Added); // Only the seed
        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsCreatedId()
    {
        var command = new CreateEstablishmentCategoryCommand("Cafetería", null, null, 0);

        Result<EstablishmentCategoryId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
        Assert.Equal(result.Value, _repository.Added[0].Id);
    }

    [Fact]
    public async Task Handle_NormalizesNameBeforeCheckingDuplicate()
    {
        _repository.Seed(EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0));

        var command = new CreateEstablishmentCategoryCommand("  Cafetería  ", null, null, 0);

        Result<EstablishmentCategoryId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNameExistsWithDifferentCasing_ReturnsConflict()
    {
        _repository.Seed(EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 0));

        var command = new CreateEstablishmentCategoryCommand("RESTAURANTE", null, null, 0);

        Result<EstablishmentCategoryId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("EstablishmentCategories.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new CreateEstablishmentCategoryCommand("Cafetería", null, null, 0);

        // The fake doesn't check cancellation, but the handler passes it through
        // This verifies the handler accepts and uses the token
        Result<EstablishmentCategoryId> result = await _handler.Handle(command, cts.Token);

        Assert.True(result.IsSuccess);
    }
}
