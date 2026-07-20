using Lyria.Application.Features.Restrictions.UpdateStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Restrictions;

public sealed class UpdateRestrictionStatusTests
{
    private readonly FakeRestrictionRepository _repository = new();
    private readonly UpdateRestrictionStatusCommandHandler _handler;

    public UpdateRestrictionStatusTests()
    {
        _handler = new UpdateRestrictionStatusCommandHandler(_repository);
    }

    private Restriction CreateAndSeed()
    {
        var restriction = Restriction.Create(RestrictionId.New(), "Vegano", null);
        _repository.Seed(restriction);
        return restriction;
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateRestrictionStatusCommand(Guid.NewGuid(), false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Activate_SetsIsActiveTrue()
    {
        var restriction = CreateAndSeed();
        restriction.Deactivate();

        var command = new UpdateRestrictionStatusCommand(restriction.Id.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(restriction.IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalse()
    {
        var restriction = CreateAndSeed();

        var command = new UpdateRestrictionStatusCommand(restriction.Id.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(restriction.IsActive);
    }

    [Fact]
    public async Task Handle_ActivateAlreadyActive_IsIdempotent()
    {
        var restriction = CreateAndSeed();
        Assert.True(restriction.IsActive);

        var command = new UpdateRestrictionStatusCommand(restriction.Id.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(restriction.IsActive);
    }

    [Fact]
    public async Task Handle_DeactivateAlreadyInactive_IsIdempotent()
    {
        var restriction = CreateAndSeed();
        restriction.Deactivate();

        var command = new UpdateRestrictionStatusCommand(restriction.Id.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(restriction.IsActive);
    }
}
