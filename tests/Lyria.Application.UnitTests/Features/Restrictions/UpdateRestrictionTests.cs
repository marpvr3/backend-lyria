using Lyria.Application.Features.Restrictions.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class UpdateRestrictionTests
{
    private readonly FakeRestrictionRepository _repository = new();
    private readonly UpdateRestrictionCommandHandler _handler;

    public UpdateRestrictionTests()
    {
        _handler = new UpdateRestrictionCommandHandler(_repository);
    }

    private Restriction CreateAndSeed(string name = "Vegano")
    {
        var restriction = Restriction.Create(RestrictionId.New(), name, null);
        _repository.Seed(restriction);
        return restriction;
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateRestrictionCommand(Guid.NewGuid(), "Vegano", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ValidInput_UpdatesAndSaves()
    {
        var restriction = CreateAndSeed();

        var command = new UpdateRestrictionCommand(restriction.Id.Value, "Vegetariano", "Sin carne.");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Vegetariano", restriction.Name);
        Assert.Equal("Sin carne.", restriction.Description);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_SameNameSameEntity_Succeeds()
    {
        var restriction = CreateAndSeed("Vegano");

        var command = new UpdateRestrictionCommand(restriction.Id.Value, "Vegano", "Actualizada.");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_DuplicateNameDifferentEntity_ReturnsConflict()
    {
        CreateAndSeed("Vegano");
        var other = CreateAndSeed("Vegetariano");

        var command = new UpdateRestrictionCommand(other.Id.Value, "Vegano", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NotFound_DoesNotSave()
    {
        var command = new UpdateRestrictionCommand(Guid.NewGuid(), "Vegano", null);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
