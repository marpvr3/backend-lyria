using Lyria.Application.Features.EstablishmentBranchRestrictions.Assign;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchRestrictions;

public sealed class AssignRestrictionToBranchTests
{
    private readonly FakeEstablishmentBranchRestrictionRepository _branchRestrictionRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly FakeRestrictionRepository _restrictionRepository = new();
    private readonly AssignRestrictionToBranchCommandHandler _handler;
    private readonly EstablishmentBranch _activeBranch;
    private readonly Restriction _activeRestriction;

    public AssignRestrictionToBranchTests()
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(),
            EstablishmentCategoryId.New(),
            "Let It V", "let-it-v",
            null, null, null, null, null, null);

        _activeBranch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(),
            establishment.Id,
            "Sede Palermo", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        _branchRepository.Seed(_activeBranch);

        _activeRestriction = Restriction.Create(RestrictionId.New(), "Vegano", "Sin productos animales.");
        _restrictionRepository.Seed(_activeRestriction);

        _handler = new AssignRestrictionToBranchCommandHandler(
            _branchRestrictionRepository, _branchRepository, _restrictionRepository);
    }

    [Fact]
    public async Task Handle_ValidInput_AssignsRestriction()
    {
        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, _activeRestriction.Id.Value,
            RestrictionComplianceLevel.Guaranteed, false, "Observación");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _branchRestrictionRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new AssignRestrictionToBranchCommand(
            Guid.NewGuid(), _activeRestriction.Id.Value,
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchInactive_ReturnsConflict()
    {
        _activeBranch.Deactivate();

        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, _activeRestriction.Id.Value,
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RestrictionNotFound_ReturnsNotFound()
    {
        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, Guid.NewGuid(),
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RestrictionInactive_ReturnsConflict()
    {
        _activeRestriction.Deactivate();

        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, _activeRestriction.Id.Value,
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Restriction.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_AlreadyExists_ReturnsConflict()
    {
        var existing = EstablishmentBranchRestriction.Create(
            _activeBranch.Id, _activeRestriction.Id,
            RestrictionComplianceLevel.Guaranteed, false, null);
        _branchRestrictionRepository.Seed(existing);

        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, _activeRestriction.Id.Value,
            RestrictionComplianceLevel.Guaranteed, false, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchRestriction.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_InvalidComplianceLevel_Throws()
    {
        var command = new AssignRestrictionToBranchCommand(
            _activeBranch.Id.Value, _activeRestriction.Id.Value,
            (RestrictionComplianceLevel)0, false, null);

        await Assert.ThrowsAsync<EstablishmentBranchException>(
            () => _handler.Handle(command, TestContext.Current.CancellationToken).AsTask());
    }
}
