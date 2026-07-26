using Lyria.Application.Features.EstablishmentBranches.UpdateStatus;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class UpdateEstablishmentBranchStatusTests
{
    private readonly FakeEstablishmentBranchRepository _repository = new();
    private readonly UpdateEstablishmentBranchStatusCommandHandler _handler;

    public UpdateEstablishmentBranchStatusTests()
    {
        _handler = new UpdateEstablishmentBranchStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new UpdateEstablishmentBranchStatusCommand(Guid.NewGuid(), true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_Activate_SetsIsActiveTrue()
    {
        var branch = CreateAndSeedBranch();
        branch.Deactivate();

        var command = new UpdateEstablishmentBranchStatusCommand(
            branch.Id.Value, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(branch.IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalse()
    {
        var branch = CreateAndSeedBranch();

        var command = new UpdateEstablishmentBranchStatusCommand(
            branch.Id.Value, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(branch.IsActive);
    }

    private EstablishmentBranch CreateAndSeedBranch()
    {
        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(),
            EstablishmentId.New(),
            "Sede Central",
            "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _repository.Seed(branch);
        return branch;
    }
}
