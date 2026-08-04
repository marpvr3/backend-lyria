using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida el alcance exacto de la migración RemoveCodeFromRoles: debe eliminar
/// únicamente el índice y la columna asociados a Code, preservando el resto de
/// la tabla Roles y sin tocar UsuarioRoles ni otras tablas.
/// </summary>
public class RemoveCodeFromRolesMigrationTests
{
    private const string MigrationFileName = "20260804000728_RemoveCodeFromRoles.cs";

    [Fact]
    public void Migration_DropsUniqueIndexOnCodigo()
    {
        string source = ReadMigrationFile();

        Assert.Contains("DropIndex", source, StringComparison.Ordinal);
        Assert.Contains("UX_Roles_Codigo", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DropsCodigoColumnFromRoles()
    {
        string source = ReadMigrationFile();

        Assert.Contains("DropColumn", source, StringComparison.Ordinal);
        Assert.Contains("name: \"Codigo\"", source, StringComparison.Ordinal);
        Assert.Contains("table: \"Roles\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DoesNotDropOrRecreateRolesTable()
    {
        string source = ReadMigrationFile();

        Assert.DoesNotContain("DropTable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateTable", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_PreservesRemainingRoleColumns()
    {
        string source = ReadMigrationFile();

        foreach (string column in new[]
        {
            "RolId",
            "Nombre",
            "Descripcion",
            "Activo",
            "FechaCreacion",
            "FechaActualizacion"
        })
        {
            Assert.DoesNotContain($"name: \"{column}\"", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_DoesNotTouchUserRolesOrOtherTables()
    {
        string source = ReadMigrationFile();

        foreach (string table in new[]
        {
            "UsuarioRoles",
            "Usuarios",
            "CategoriasEstablecimiento",
            "Establecimientos",
            "Sedes",
            "Restricciones",
            "Servicios",
            "LogsAplicacion"
        })
        {
            Assert.DoesNotContain(table, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_DoesNotAlterKeysOrForeignKeys()
    {
        string source = ReadMigrationFile();

        foreach (string api in new[]
        {
            "DropPrimaryKey",
            "AddPrimaryKey",
            "DropForeignKey",
            "AddForeignKey"
        })
        {
            Assert.DoesNotContain(api, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_DoesNotSeedOrMutateRoleData()
    {
        string source = ReadMigrationFile();

        foreach (string api in new[] { "InsertData", "UpdateData", "DeleteData", "Sql(" })
        {
            Assert.DoesNotContain(api, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Snapshot_DoesNotContainRoleCodeMapping()
    {
        string snapshot = ReadSnapshotFile();

        Assert.DoesNotContain("UX_Roles_Codigo", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain("HasColumnName(\"Codigo\")", snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoricalMigration_StillDeclaresCodigoColumn()
    {
        // La migración histórica que creó la tabla no debe editarse.
        string source = ReadMigrationFile("20260727064949_AddUsersRolesAndUserRoles.cs");

        Assert.Contains("Codigo", source, StringComparison.Ordinal);
        Assert.Contains("UX_Roles_Codigo", source, StringComparison.Ordinal);
    }

    private static string ReadMigrationFile(string fileName = MigrationFileName)
    {
        string path = Path.Combine(
            FindSourceDirectory(), "src", "Lyria.Infrastructure",
            "Persistence", "Migrations", fileName);

        Assert.True(File.Exists(path), $"No se encontró la migración en: {path}");

        return File.ReadAllText(path);
    }

    private static string ReadSnapshotFile()
    {
        string path = Path.Combine(
            FindSourceDirectory(), "src", "Lyria.Infrastructure",
            "Persistence", "Migrations", "LyriaDbContextModelSnapshot.cs");

        Assert.True(File.Exists(path), $"No se encontró el snapshot en: {path}");

        return File.ReadAllText(path);
    }

    private static string FindSourceDirectory()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "Lyria.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }
}
