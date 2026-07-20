using Lyria.Application.Features.EstablishmentBranchServices.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class UpdateBranchServiceTests
{
    private readonly FakeEstablishmentBranchServiceRepository _repository = new();
    private readonly UpdateBranchServiceCommandHandler _handler;
    private readonly EstablishmentBranchId _branchId = EstablishmentBranchId.New();
    private readonly ServiceId _serviceId = ServiceId.New();

    public UpdateBranchServiceTests()
    {
        var existing = EstablishmentBranchService.Create(_branchId, _serviceId, true, null);
        _repository.Seed(existing);

        _handler = new UpdateBranchServiceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesAvailability()
    {
        var command = new UpdateBranchServiceCommand(
            _branchId.Value, _serviceId.Value, false, "Suspendido");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesObservation()
    {
        var command = new UpdateBranchServiceCommand(
            _branchId.Value, _serviceId.Value, true, "Nueva observación");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchServiceCommand(
            Guid.NewGuid(), Guid.NewGuid(), true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchService.NotFound", result.Error.Code);
    }
}
