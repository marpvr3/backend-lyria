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
    public void Configuration_ShouldMapExpectedColumns()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;

        string[] columns = entityType.GetProperties()
            .Select(p => p.GetColumnName())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "Activo",
            "Descripcion",
            "FechaActualizacion",
            "FechaCreacion",
            "Nombre",
            "RolId"
        ];

        Assert.Equal(expected, columns);
    }

    [Fact]
    public void Configuration_ShouldNotMapCode()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;

        Assert.Null(entityType.FindProperty("Code"));
        Assert.DoesNotContain(
            entityType.GetProperties(),
            p => p.GetColumnName() == "Codigo");
    }

    [Fact]
    public void Configuration_ShouldNotDeclareAnyCodeIndex()
    {
        using var context = _fixture.CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Role))!;

        Assert.DoesNotContain(
            entityType.GetIndexes(),
            i => i.GetDatabaseName()!.Contains("Codigo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PhysicalTable_ShouldNotContainCodigoColumn()
    {
        using var context = _fixture.CreateContext();

        List<string> columns = ReadPhysicalColumns(context);

        Assert.DoesNotContain("Codigo", columns);
        Assert.Contains("RolId", columns);
        Assert.Contains("Nombre", columns);
        Assert.Contains("Descripcion", columns);
        Assert.Contains("Activo", columns);
        Assert.Contains("FechaCreacion", columns);
        Assert.Contains("FechaActualizacion", columns);
    }

    private static List<string> ReadPhysicalColumns(Lyria.Infrastructure.Persistence.LyriaDbContext context)
    {
        var connection = context.Database.GetDbConnection();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Roles');";

        var columns = new List<string>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
