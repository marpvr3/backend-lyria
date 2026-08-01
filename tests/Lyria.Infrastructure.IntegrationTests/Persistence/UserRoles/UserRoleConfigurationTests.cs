using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.UserRoles;

public sealed class UserRoleConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_ShouldBeNamedUsuarioRoles()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserRole))!;

        Assert.Equal("UsuarioRoles", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_ShouldBeUsuarioRolId()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserRole))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_UsuarioRoles", primaryKey.GetName());

        var idProperty = entityType.FindProperty(nameof(UserRole.Id))!;
        Assert.Equal("UsuarioRolId", idProperty.GetColumnName());
    }

    [Fact]
    public void Should_Have_FK_To_Usuarios()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserRole))!;
        var foreignKeys = entityType.GetForeignKeys().ToList();

        var userFk = foreignKeys.SingleOrDefault(
            fk => fk.GetConstraintName() == "FK_UsuarioRoles_Usuarios_UsuarioId");

        Assert.NotNull(userFk);
    }

    [Fact]
    public void Should_Have_FK_To_Roles()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(UserRole))!;
        var foreignKeys = entityType.GetForeignKeys().ToList();

        var roleFk = foreignKeys.SingleOrDefault(
            fk => fk.GetConstraintName() == "FK_UsuarioRoles_Roles_RolId");

        Assert.NotNull(roleFk);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
