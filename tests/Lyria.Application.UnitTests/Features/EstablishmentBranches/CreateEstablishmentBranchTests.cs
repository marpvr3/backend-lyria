using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentBranches.Create;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranches;

public sealed class CreateEstablishmentBranchTests
{
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly FakeEstablishmentRepository _establishmentRepository = new();
    private readonly FakeTimeZoneService _timeZoneService = new();
    private readonly CreateEstablishmentBranchCommandHandler _handler;
    private readonly Establishment _activeEstablishment;

    public CreateEstablishmentBranchTests()
    {
        _activeEstablishment = Establishment.Create(
            EstablishmentId.New(),
            EstablishmentCategoryId.New(),
            "Let It V",
            "let-it-v",
            null, null, null, null, null, null);
        _establishmentRepository.Seed(_activeEstablishment);

        var timeZoneDefaults = new FakeBranchTimeZoneDefaults();

        _handler = new CreateEstablishmentBranchCommandHandler(
            _branchRepository, _establishmentRepository, _timeZoneService, timeZoneDefaults);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesBranch()
    {
        var command = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Costa Rica 5865",
            "Piso 2", null, "Palermo", "Buenos Aires", "Buenos Aires",
            "C1414", "Argentina", -34.5795m, -58.4321m,
            "+54 11 5555-0001", "+54 11 5555-0002", "sede@letitv.com", null);

        Result<EstablishmentBranchId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_branchRepository.Added);
        Assert.Equal(1, _branchRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_EstablishmentNotFound_ReturnsNotFound()
    {
        var command = new CreateEstablishmentBranchCommand(
            Guid.NewGuid(), "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null, null);

        Result<EstablishmentBranchId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("Establishment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_EstablishmentInactive_ReturnsValidation()
    {
        _activeEstablishment.Deactivate();

        var command = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null, null);

        Result<EstablishmentBranchId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("Establishment.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NameDuplicate_ReturnsConflict()
    {
        var first = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Av. Corrientes 1234",
            null, null, null, null, null, null, null, null, null, null, null, null, null);
        await _handler.Handle(first, CancellationToken.None);

        var second = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Av. Santa Fe 5678",
            null, null, null, null, null, null, null, null, null, null, null, null, null);
        Result<EstablishmentBranchId> result = await _handler.Handle(second, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_SameNameDifferentEstablishment_Succeeds()
    {
        var otherEstablishment = Establishment.Create(
            EstablishmentId.New(),
            EstablishmentCategoryId.New(),
            "Other Place",
            "other-place",
            null, null, null, null, null, null);
        _establishmentRepository.Seed(otherEstablishment);

        var otherBranchRepo = new FakeEstablishmentBranchRepository();
        var otherHandler = new CreateEstablishmentBranchCommandHandler(
            otherBranchRepo, _establishmentRepository, _timeZoneService, new FakeBranchTimeZoneDefaults());

        var firstCommand = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Av. Corrientes 1234",
            null, null, null, null, null, null, null, null, null, null, null, null, null);
        await _handler.Handle(firstCommand, CancellationToken.None);

        var secondCommand = new CreateEstablishmentBranchCommand(
            otherEstablishment.Id.Value, "Sede Central", "Av. Santa Fe 5678",
            null, null, null, null, null, null, null, null, null, null, null, null, null);
        Result<EstablishmentBranchId> result = await otherHandler.Handle(secondCommand, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_StartsActive()
    {
        var command = new CreateEstablishmentBranchCommand(
            _activeEstablishment.Id.Value, "Sede Central", "Costa Rica 5865",
            null, null, null, null, null, null, null, null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        var created = _branchRepository.Added[0];
        Assert.True(created.IsActive);
    }
}
