using Lyria.Application.Features.Establishments.UpdateStatus;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Establishments;

public sealed class UpdateEstablishmentStatusTests
{
    private readonly FakeEstablishmentRepository _repository = new();
    private readonly UpdateEstablishmentStatusCommandHandler _handler;

    public UpdateEstablishmentStatusTests()
    {
        _handler = new UpdateEstablishmentStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new UpdateEstablishmentStatusCommand(Guid.NewGuid(), true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenNotFound_DoesNotSave()
    {
        var command = new UpdateEstablishmentStatusCommand(Guid.NewGuid(), true);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Activate_SetsIsActiveTrue()
    {
        var establishment = CreateAndSeedEstablishment();
        establishment.Deactivate();

        var command = new UpdateEstablishmentStatusCommand(
            establishment.Id.Value, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(establishment.IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalse()
    {
        var establishment = CreateAndSeedEstablishment();

        var command = new UpdateEstablishmentStatusCommand(
            establishment.Id.Value, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(establishment.IsActive);
    }

    [Fact]
    public async Task Handle_Activate_IsIdempotent()
    {
        var establishment = CreateAndSeedEstablishment();

        var command = new UpdateEstablishmentStatusCommand(
            establishment.Id.Value, true);

        await _handler.Handle(command, CancellationToken.None);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(establishment.IsActive);
    }

    [Fact]
    public async Task Handle_Deactivate_IsIdempotent()
    {
        var establishment = CreateAndSeedEstablishment();
        establishment.Deactivate();

        var command = new UpdateEstablishmentStatusCommand(
            establishment.Id.Value, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(establishment.IsActive);
    }

    private Establishment CreateAndSeedEstablishment()
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(),
            EstablishmentCategoryId.New(),
            "Test Place",
            "test-place",
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
