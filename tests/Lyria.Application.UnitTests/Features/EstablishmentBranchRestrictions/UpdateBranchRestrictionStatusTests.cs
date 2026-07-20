using Lyria.Application.Features.EstablishmentBranchRestrictions.UpdateStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class UpdateBranchRestrictionStatusTests
{
    private readonly FakeEstablishmentBranchRestrictionRepository _repository = new();
    private readonly UpdateBranchRestrictionStatusCommandHandler _handler;
    private readonly EstablishmentBranchId _branchId = EstablishmentBranchId.New();
    private readonly RestrictionId _restrictionId = RestrictionId.New();

    public UpdateBranchRestrictionStatusTests()
    {
        var existing = EstablishmentBranchRestriction.Create(
            _branchId, _restrictionId, RestrictionComplianceLevel.Guaranteed, false, null);
        _repository.Seed(existing);

        _handler = new UpdateBranchRestrictionStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Activate_Succeeds()
    {
        var command = new UpdateBranchRestrictionStatusCommand(
            _branchId.Value, _restrictionId.Value, true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Deactivate_Succeeds()
    {
        var command = new UpdateBranchRestrictionStatusCommand(
            _branchId.Value, _restrictionId.Value, false);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchRestrictionStatusCommand(
            Guid.NewGuid(), Guid.NewGuid(), true);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchRestriction.NotFound", result.Error.Code);
    }
}
