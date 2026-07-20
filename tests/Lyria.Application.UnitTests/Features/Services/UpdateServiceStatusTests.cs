using Lyria.Application.Features.Services.UpdateStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class UpdateServiceStatusTests
{
    private readonly FakeServiceRepository _repository = new();
    private readonly UpdateServiceStatusCommandHandler _handler;

    public UpdateServiceStatusTests()
    {
        _handler = new UpdateServiceStatusCommandHandler(_repository);
    }

    private Service CreateAndSeed()
    {
        var service = Service.Create(ServiceId.New(), "Delivery", null, null);
        _repository.Seed(service);
        return service;
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateServiceStatusCommand(Guid.NewGuid(), false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Activate_SetsIsActiveTrue()
    {
        var service = CreateAndSeed();
        service.Deactivate();

        var command = new UpdateServiceStatusCommand(service.Id.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalse()
    {
        var service = CreateAndSeed();

        var command = new UpdateServiceStatusCommand(service.Id.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
    }

    [Fact]
    public async Task Handle_ActivateAlreadyActive_IsIdempotent()
    {
        var service = CreateAndSeed();
        Assert.True(service.IsActive);

        var command = new UpdateServiceStatusCommand(service.Id.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
    }

    [Fact]
    public async Task Handle_DeactivateAlreadyInactive_IsIdempotent()
    {
        var service = CreateAndSeed();
        service.Deactivate();

        var command = new UpdateServiceStatusCommand(service.Id.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
    }
}
