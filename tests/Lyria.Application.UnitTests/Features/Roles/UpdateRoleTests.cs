using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Roles.Update;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Roles;

public sealed class UpdateRoleTests
{
    private readonly FakeRoleRepository _repository = new();
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleTests()
    {
        _handler = new UpdateRoleCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_UpdatesRole()
    {
        var roleId = RoleId.New();
        _repository.Seed(Role.Create(
            roleId, "Administrador", null));

        var command = new UpdateRoleCommand(
            roleId.Value, "Super Administrador", "Descripción actualizada");

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Super Administrador", _repository.Added[0].Name);
        Assert.Equal("Descripción actualizada", _repository.Added[0].Description);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsNotFound()
    {
        var command = new UpdateRoleCommand(
            Guid.NewGuid(), "Super Admin", null);

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Roles.NotFound", result.Error.Code);
    }
}
