using System.Reflection;
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
    public async Task Handle_WithNameAndDescription_CreatesRole()
    {
        var command = new CreateRoleCommand("Administrador", "Rol de administrador");

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Added);
        Assert.Equal("Administrador", _repository.Added[0].Name);
        Assert.Equal("Rol de administrador", _repository.Added[0].Description);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithoutDescription_CreatesRole()
    {
        var command = new CreateRoleCommand("Administrador", null);

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(_repository.Added[0].Description);
    }

    [Fact]
    public async Task Handle_NormalizesNameBeforePersisting()
    {
        var command = new CreateRoleCommand("  Super   Admin  ", null);

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Super Admin", _repository.Added[0].Name);
    }

    [Fact]
    public async Task Handle_WithExistingRoleName_DoesNotConflict()
    {
        _repository.Seed(Role.Create(RoleId.New(), "Administrador", null));

        var command = new CreateRoleCommand("Administrador", null);

        Result<RoleId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Command_DoesNotExposeCode()
    {
        string[] parameters = typeof(CreateRoleCommand)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.Equal(["Name", "Description"], parameters);
    }
}
