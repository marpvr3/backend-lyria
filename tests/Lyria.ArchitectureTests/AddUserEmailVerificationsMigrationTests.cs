using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Valida el alcance exacto de la migración AddUserEmailVerifications: debe crear
/// únicamente dbo.UsuarioVerificacionesCorreo con sus columnas, PK, FK e índice, sin
/// tocar ninguna otra tabla ni ningún dato existente.
/// </summary>
public class AddUserEmailVerificationsMigrationTests
{
    private const string MigrationFileName = "20260806011115_AddUserEmailVerifications.cs";

    private static readonly string[] UntouchedTables =
    [
        "Usuarios",
        "Roles",
        "UsuarioRoles",
        "Restricciones",
        "UsuarioRestricciones",
        "UsuarioRefreshTokens",
        "CategoriasEstablecimiento",
        "Establecimientos",
        "Sedes",
        "Servicios",
        "LogsAplicacion"
    ];

    [Fact]
    public void Migration_CreatesOnlyTheEmailVerificationsTable()
    {
        string source = ReadMigrationFile();

        Assert.Contains("CreateTable", source, StringComparison.Ordinal);
        Assert.Contains(
            "name: \"UsuarioVerificacionesCorreo\"", source, StringComparison.Ordinal);

        // Una sola invocación de CreateTable en toda la migración.
        Assert.Equal(1, CountOccurrences(source, "migrationBuilder.CreateTable"));
    }

    [Fact]
    public void Migration_DeclaresAllTheExpectedColumns()
    {
        string source = ReadMigrationFile();

        foreach (string column in new[]
        {
            "VerificacionCorreoId",
            "UsuarioId",
            "CodigoHash",
            "FechaCreacion",
            "FechaExpiracion",
            "FechaUso",
            "FechaRevocacion",
            "IntentosFallidos"
        })
        {
            Assert.Contains(column, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_UsesTheExpectedPhysicalTypes()
    {
        string source = ReadMigrationFile();

        Assert.Contains("type: \"uniqueidentifier\"", source, StringComparison.Ordinal);
        Assert.Contains("type: \"varchar(64)\"", source, StringComparison.Ordinal);
        Assert.Contains("type: \"datetime2\"", source, StringComparison.Ordinal);
        Assert.Contains("type: \"int\"", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// El código en claro no puede tener columna: solo se almacena su hash.
    /// </summary>
    [Fact]
    public void Migration_DeclaresNoPlainCodeColumn()
    {
        string source = ReadMigrationFile();

        Assert.DoesNotContain("Codigo =", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Correo =", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Contrasena", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DeclaresThePrimaryKey()
    {
        string source = ReadMigrationFile();

        Assert.Contains(
            "table.PrimaryKey(\"PK_UsuarioVerificacionesCorreo\"",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DeclaresTheForeignKeyWithRestrict()
    {
        string source = ReadMigrationFile();

        Assert.Contains(
            "FK_UsuarioVerificacionesCorreo_Usuarios_UsuarioId",
            source,
            StringComparison.Ordinal);
        Assert.Contains("principalTable: \"Usuarios\"", source, StringComparison.Ordinal);
        Assert.Contains("principalColumn: \"UsuarioId\"", source, StringComparison.Ordinal);
        Assert.Contains(
            "onDelete: ReferentialAction.Restrict", source, StringComparison.Ordinal);

        // Sin borrado en cascada bajo ninguna forma.
        Assert.DoesNotContain("ReferentialAction.Cascade", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_DeclaresTheUserIdIndex()
    {
        string source = ReadMigrationFile();

        Assert.Contains(
            "IX_UsuarioVerificacionesCorreo_UsuarioId", source, StringComparison.Ordinal);

        // Sin índice único: un usuario acumula verificaciones históricas.
        Assert.DoesNotContain("unique: true", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// La migración no puede alterar ninguna tabla existente. La única mención admitida
    /// de "Usuarios" es la de la clave foránea.
    /// </summary>
    [Fact]
    public void Migration_DoesNotModifyAnyExistingTable()
    {
        string source = ReadMigrationFile();

        foreach (string api in new[]
        {
            "AddColumn",
            "DropColumn",
            "AlterColumn",
            "RenameColumn",
            "RenameTable",
            "DropIndex",
            "DropPrimaryKey",
            "DropForeignKey"
        })
        {
            Assert.DoesNotContain(api, source, StringComparison.Ordinal);
        }

        foreach (string table in UntouchedTables.Where(t => t != "Usuarios"))
        {
            Assert.DoesNotContain(table, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_DoesNotSeedOrMutateData()
    {
        string source = ReadMigrationFile();

        foreach (string api in new[] { "InsertData", "UpdateData", "DeleteData", "Sql(" })
        {
            Assert.DoesNotContain(api, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_DownOnlyDropsTheNewTable()
    {
        string source = ReadMigrationFile();

        Assert.Contains("DropTable", source, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(source, "migrationBuilder.DropTable"));
    }

    [Fact]
    public void Snapshot_ContainsTheEmailVerificationMapping()
    {
        string snapshot = ReadSnapshotFile();

        Assert.Contains("UsuarioVerificacionesCorreo", snapshot, StringComparison.Ordinal);
        Assert.Contains("CodigoHash", snapshot, StringComparison.Ordinal);
        Assert.Contains(
            "IX_UsuarioVerificacionesCorreo_UsuarioId", snapshot, StringComparison.Ordinal);
    }

    /// <summary>
    /// Las migraciones históricas no deben editarse.
    /// </summary>
    [Fact]
    public void HistoricalMigrations_AreNotModified()
    {
        foreach (string fileName in new[]
        {
            "20260727064949_AddUsersRolesAndUserRoles.cs",
            "20260802055758_AddUserRestrictions.cs",
            "20260805003639_AddUserRefreshTokens.cs"
        })
        {
            Assert.DoesNotContain(
                "UsuarioVerificacionesCorreo",
                ReadMigrationFile(fileName),
                StringComparison.Ordinal);
        }
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = source.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
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
