using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Roles.Create;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Roles;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Roles;

public sealed class CreateRoleTests
{
    private readonly FakeRoleRepository _repository = new();
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleTests()
    {
        _handler = new CreateRoleCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenCodeDoesNotExist_CreatesRole()
    {
        var command = new CreateRoleCommand("ADMIN", "Administrador", "Rol de administrador");

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Added);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenCodeExists_ReturnsConflict()
    {
        _repository.Seed(Role.Create(
            RoleId.New(), "ADMIN", "Administrador", null));

        var command = new CreateRoleCommand("ADMIN", "Otro Admin", null);

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Roles.CodeAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NormalizesCodeBeforeCheckingDuplicate()
    {
        _repository.Seed(Role.Create(
            RoleId.New(), "ADMIN", "Administrador", null));

        var command = new CreateRoleCommand("  admin  ", "Otro Admin", null);

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}
