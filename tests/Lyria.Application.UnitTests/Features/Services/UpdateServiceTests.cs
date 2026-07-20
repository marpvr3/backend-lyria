using Lyria.Application.Features.Services.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class UpdateServiceTests
{
    private readonly FakeServiceRepository _repository = new();
    private readonly UpdateServiceCommandHandler _handler;

    public UpdateServiceTests()
    {
        _handler = new UpdateServiceCommandHandler(_repository);
    }

    private Service CreateAndSeed(string name = "Delivery")
    {
        var service = Service.Create(ServiceId.New(), name, null, null);
        _repository.Seed(service);
        return service;
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateServiceCommand(Guid.NewGuid(), "Delivery", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ValidInput_UpdatesAndSaves()
    {
        var service = CreateAndSeed();

        var command = new UpdateServiceCommand(service.Id.Value, "Wi-Fi", "Conexión inalámbrica.", "https://cdn.lyria.com/icons/wifi.svg");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Wi-Fi", service.Name);
        Assert.Equal("Conexión inalámbrica.", service.Description);
        Assert.Equal("https://cdn.lyria.com/icons/wifi.svg", service.IconUrl);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_SameNameSameEntity_Succeeds()
    {
        var service = CreateAndSeed("Delivery");

        var command = new UpdateServiceCommand(service.Id.Value, "Delivery", "Actualizada.", null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_DuplicateNameDifferentEntity_ReturnsConflict()
    {
        CreateAndSeed("Delivery");
        var other = CreateAndSeed("Wi-Fi");

        var command = new UpdateServiceCommand(other.Id.Value, "Delivery", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NotFound_DoesNotSave()
    {
        var command = new UpdateServiceCommand(Guid.NewGuid(), "Delivery", null, null);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
