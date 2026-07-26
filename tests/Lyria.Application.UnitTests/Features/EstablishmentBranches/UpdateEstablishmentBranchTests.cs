using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentBranches.Update;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class UpdateEstablishmentBranchTests
{
    private readonly FakeEstablishmentBranchRepository _repository = new();
    private readonly UpdateEstablishmentBranchCommandHandler _handler;

    public UpdateEstablishmentBranchTests()
    {
        _handler = new UpdateEstablishmentBranchCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var command = new UpdateEstablishmentBranchCommand(
            Guid.NewGuid(), "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ValidInput_UpdatesAndSaves()
    {
        var branch = CreateAndSeedBranch("Sede Original");

        var command = new UpdateEstablishmentBranchCommand(
            branch.Id.Value, "Sede Actualizada", "Av. Santa Fe 1234",
            "Local 5", null, "Recoleta", "Buenos Aires", "Buenos Aires",
            "C1060", "Argentina", -34.5950m, -58.3930m,
            "+54 11 5555-9999", null, "nueva@letitv.com");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sede Actualizada", branch.Name);
        Assert.Equal("Av. Santa Fe 1234", branch.Street);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_NameDuplicate_ReturnsConflict()
    {
        var establishmentId = EstablishmentId.New();
        var first = CreateAndSeedBranch("Sede Norte", establishmentId);
        var second = CreateAndSeedBranch("Sede Sur", establishmentId);

        var command = new UpdateEstablishmentBranchCommand(
            second.Id.Value, "Sede Norte", "Av. Corrientes 1234",
            null, null, null, null, null, null, null, null, null, null, null, null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    private EstablishmentBranch CreateAndSeedBranch(
        string name = "Sede Central",
        EstablishmentId? establishmentId = null)
    {
        var branch = EstablishmentBranch.Create(
            EstablishmentBranchId.New(),
            establishmentId ?? EstablishmentId.New(),
            name,
            "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null, "America/Argentina/Buenos_Aires");
        _repository.Seed(branch);
        return branch;
    }
}
