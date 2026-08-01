using Lyria.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Persistence.Roles;

public sealed class RoleConfigurationTests : IDisposable
{
    private readonly SqliteFixture _fixture = new();

    [Fact]
    public void Table_ShouldBeNamedRoles()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;

        Assert.Equal("Roles", entityType.GetTableName());
    }

    [Fact]
    public void PrimaryKey_ShouldBeRolId()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;
        var primaryKey = entityType.FindPrimaryKey()!;

        Assert.Equal("PK_Roles", primaryKey.GetName());

        var idProperty = entityType.FindProperty(nameof(Role.Id))!;
        Assert.Equal("RolId", idProperty.GetColumnName());
    }

    [Fact]
    public void Code_ShouldHaveUniqueIndex()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;
        var indexes = entityType.GetIndexes().ToList();

        var codeIndex = indexes.SingleOrDefault(
            i => i.GetDatabaseName() == "UX_Roles_Codigo");

        Assert.NotNull(codeIndex);
        Assert.True(codeIndex.IsUnique);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
