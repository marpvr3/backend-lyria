using Lyria.Application.Features.Services.Create;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Services;

public sealed class CreateServiceTests
{
    private readonly FakeServiceRepository _repository = new();
    private readonly CreateServiceCommandHandler _handler;

    public CreateServiceTests()
    {
        _handler = new CreateServiceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesService()
    {
        var command = new CreateServiceCommand("Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var existing = Service.Create(ServiceId.New(), "Delivery", null, null);
        _repository.Seed(existing);

        var command = new CreateServiceCommand("Delivery", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DuplicateNameDifferentCase_ReturnsConflict()
    {
        var existing = Service.Create(ServiceId.New(), "Delivery", null, null);
        _repository.Seed(existing);

        var command = new CreateServiceCommand("delivery", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UniqueName_Succeeds()
    {
        var existing = Service.Create(ServiceId.New(), "Delivery", null, null);
        _repository.Seed(existing);

        var command = new CreateServiceCommand("Wi-Fi", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsCreatedId()
    {
        var command = new CreateServiceCommand("Delivery", null, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
    }

    [Fact]
    public async Task Handle_Error_DoesNotAddOrSave()
    {
        var existing = Service.Create(ServiceId.New(), "Delivery", null, null);
        _repository.Seed(existing);

        var command = new CreateServiceCommand("Delivery", null, null);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }
}
