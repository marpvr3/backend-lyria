using Lyria.Application.Features.EstablishmentBranchServices.Assign;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EstablishmentBranchServices;

public sealed class AssignServiceToBranchTests
{
    private readonly FakeEstablishmentBranchServiceRepository _branchServiceRepository = new();
    private readonly FakeEstablishmentBranchRepository _branchRepository = new();
    private readonly FakeServiceRepository _serviceRepository = new();
    private readonly AssignServiceToBranchCommandHandler _handler;
    private readonly EstablishmentBranch _activeBranch;
    private readonly Service _activeService;

    public AssignServiceToBranchTests()
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

        _activeService = Service.Create(ServiceId.New(), "Delivery", "Entrega a domicilio.", null);
        _serviceRepository.Seed(_activeService);

        _handler = new AssignServiceToBranchCommandHandler(
            _branchServiceRepository, _branchRepository, _serviceRepository);
    }

    [Fact]
    public async Task Handle_ValidInput_AssignsService()
    {
        var command = new AssignServiceToBranchCommand(
            _activeBranch.Id.Value, _activeService.Id.Value, true, "Observación");

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _branchServiceRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var command = new AssignServiceToBranchCommand(
            Guid.NewGuid(), _activeService.Id.Value, true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchInactive_ReturnsConflict()
    {
        _activeBranch.Deactivate();

        var command = new AssignServiceToBranchCommand(
            _activeBranch.Id.Value, _activeService.Id.Value, true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ServiceNotFound_ReturnsNotFound()
    {
        var command = new AssignServiceToBranchCommand(
            _activeBranch.Id.Value, Guid.NewGuid(), true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ServiceInactive_ReturnsConflict()
    {
        _activeService.Deactivate();

        var command = new AssignServiceToBranchCommand(
            _activeBranch.Id.Value, _activeService.Id.Value, true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Service.Inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_AlreadyExists_ReturnsConflict()
    {
        var existing = EstablishmentBranchService.Create(
            _activeBranch.Id, _activeService.Id, true, null);
        _branchServiceRepository.Seed(existing);

        var command = new AssignServiceToBranchCommand(
            _activeBranch.Id.Value, _activeService.Id.Value, true, null);

        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranchService.AlreadyExists", result.Error.Code);
    }
}
