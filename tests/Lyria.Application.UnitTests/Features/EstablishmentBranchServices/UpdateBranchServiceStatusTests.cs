using Lyria.Application.Features.EstablishmentBranchServices.UpdateStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class UpdateBranchServiceStatusTests
{
    private readonly FakeEstablishmentBranchServiceRepository _repository = new();
    private readonly UpdateBranchServiceStatusCommandHandler _handler;
    private readonly EstablishmentBranchId _branchId = EstablishmentBranchId.New();
    private readonly ServiceId _serviceId = ServiceId.New();

    public UpdateBranchServiceStatusTests()
    {
        var existing = EstablishmentBranchService.Create(_branchId, _serviceId, true, null);
        _repository.Seed(existing);

        _handler = new UpdateBranchServiceStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Activate_Succeeds()
    {
        var command = new UpdateBranchServiceStatusCommand(
            _branchId.Value, _serviceId.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Deactivate_Succeeds()
    {
        var command = new UpdateBranchServiceStatusCommand(
            _branchId.Value, _serviceId.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchServiceStatusCommand(
            Guid.NewGuid(), Guid.NewGuid(), true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchService.NotFound", result.Error.Code);
    }
}
