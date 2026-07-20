using Lyria.Application.Features.EstablishmentBranchRestrictions.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class UpdateBranchRestrictionTests
{
    private readonly FakeEstablishmentBranchRestrictionRepository _repository = new();
    private readonly UpdateBranchRestrictionCommandHandler _handler;
    private readonly EstablishmentBranchId _branchId = EstablishmentBranchId.New();
    private readonly RestrictionId _restrictionId = RestrictionId.New();

    public UpdateBranchRestrictionTests()
    {
        var existing = EstablishmentBranchRestriction.Create(
            _branchId, _restrictionId, RestrictionComplianceLevel.Guaranteed, false, null);
        _repository.Seed(existing);

        _handler = new UpdateBranchRestrictionCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesComplianceLevel()
    {
        var command = new UpdateBranchRestrictionCommand(
            _branchId.Value, _restrictionId.Value,
            RestrictionComplianceLevel.Partial, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesIsCertified()
    {
        var command = new UpdateBranchRestrictionCommand(
            _branchId.Value, _restrictionId.Value,
            RestrictionComplianceLevel.Guaranteed, true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesObservation()
    {
        var command = new UpdateBranchRestrictionCommand(
            _branchId.Value, _restrictionId.Value,
            RestrictionComplianceLevel.Guaranteed, false, "Nueva observación");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var command = new UpdateBranchRestrictionCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchRestriction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DoesNotChangeIds()
    {
        var command = new UpdateBranchRestrictionCommand(
            _branchId.Value, _restrictionId.Value,
            RestrictionComplianceLevel.OnRequest, true, "Cambio");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var entity = await _repository.GetByIdsAsync(_branchId, _restrictionId, CancellationToken.None);
        Assert.NotNull(entity);
        Assert.Equal(_branchId, entity.BranchId);
        Assert.Equal(_restrictionId, entity.RestrictionId);
    }
}
