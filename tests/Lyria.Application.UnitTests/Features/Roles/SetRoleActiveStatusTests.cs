using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Roles.SetActiveStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Roles;

public sealed class SetRoleActiveStatusTests
{
    private readonly FakeRoleRepository _repository = new();
    private readonly SetRoleActiveStatusCommandHandler _handler;

    public SetRoleActiveStatusTests()
    {
        _handler = new SetRoleActiveStatusCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Activate_WhenInactive_Succeeds()
    {
        var roleId = RoleId.New();
        var role = Role.Create(roleId, "ADMIN", "Administrador", null);
        role.Deactivate();
        _repository.Seed(role);

        var command = new SetRoleActiveStatusCommand(roleId.Value, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(_repository.Added[0].IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Deactivate_WhenActive_Succeeds()
    {
        var roleId = RoleId.New();
        _repository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        var command = new SetRoleActiveStatusCommand(roleId.Value, false);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(_repository.Added[0].IsActive);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ReturnsError()
    {
        var roleId = RoleId.New();
        _repository.Seed(Role.Create(roleId, "ADMIN", "Administrador", null));

        // Role starts as Active, trying to activate again should fail
        var command = new SetRoleActiveStatusCommand(roleId.Value, true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Roles.InvalidStatusChange", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsNotFound()
    {
        var command = new SetRoleActiveStatusCommand(Guid.NewGuid(), true);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Roles.NotFound", result.Error.Code);
    }
}
