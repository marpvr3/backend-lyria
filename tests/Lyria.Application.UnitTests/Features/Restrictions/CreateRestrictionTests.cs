using Lyria.Application.Features.Restrictions.Create;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class CreateRestrictionTests
{
    private readonly FakeRestrictionRepository _repository = new();
    private readonly CreateRestrictionCommandHandler _handler;

    public CreateRestrictionTests()
    {
        _handler = new CreateRestrictionCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesRestriction()
    {
        var command = new CreateRestrictionCommand("Vegano", "Sin productos de origen animal.");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var existing = Restriction.Create(RestrictionId.New(), "Vegano", null);
        _repository.Seed(existing);

        var command = new CreateRestrictionCommand("Vegano", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DuplicateNameDifferentCase_ReturnsConflict()
    {
        var existing = Restriction.Create(RestrictionId.New(), "Sin TACC", null);
        _repository.Seed(existing);

        var command = new CreateRestrictionCommand("sin tacc", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UniqueName_Succeeds()
    {
        var existing = Restriction.Create(RestrictionId.New(), "Vegano", null);
        _repository.Seed(existing);

        var command = new CreateRestrictionCommand("Vegetariano", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsCreatedId()
    {
        var command = new CreateRestrictionCommand("Vegano", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
    }

    [Fact]
    public async Task Handle_Error_DoesNotAddOrSave()
    {
        var existing = Restriction.Create(RestrictionId.New(), "Vegano", null);
        _repository.Seed(existing);

        var command = new CreateRestrictionCommand("Vegano", null);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
