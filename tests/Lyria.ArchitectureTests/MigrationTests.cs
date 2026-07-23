using System.Reflection;
using Xunit;

namespace Lyria.ArchitectureTests;

public class MigrationTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Migration_CreatesLogsAplicacionTable()
    {
        string migrationSource = ReadMigrationFile();

        Assert.Contains("LogsAplicacion", migrationSource);
        Assert.Contains("CREATE TABLE", migrationSource);
    }

    [Fact]
    public void Migration_CreatesPrimaryKey()
    {
        string migrationSource = ReadMigrationFile();

        Assert.Contains("PK_LogsAplicacion", migrationSource);
    }

    [Fact]
    public void Migration_CreatesThreeIndexes()
    {
        string migrationSource = ReadMigrationFile();

        Assert.Contains("IX_LogsAplicacion_FechaUtc", migrationSource);
        Assert.Contains("IX_LogsAplicacion_TraceId", migrationSource);
        Assert.Contains("IX_LogsAplicacion_CodigoRespuesta_FechaUtc", migrationSource);
    }

    [Fact]
    public void Migration_DownDropsTable()
    {
        string migrationSource = ReadMigrationFile();

        Assert.Contains("DROP TABLE", migrationSource);
        Assert.Contains("LogsAplicacion", migrationSource);
    }

    [Fact]
    public void Migration_TableAndColumnOptionsCoincide()
    {
        string migrationSource = ReadMigrationFile();

        string[] expectedColumns =
        [
            "LogAplicacionId",
            "Mensaje",
            "PlantillaMensaje",
            "Nivel",
            "FechaUtc",
            "Excepcion",
            "LogEvent",
            "TraceId",
            "SpanId",
            "TipoEvento",
            "MetodoHttp",
            "Ruta",
            "CodigoRespuesta",
            "DuracionMs",
            "RequestBody",
            "ResponseBody",
            "TipoExcepcion",
            "SourceContext",
            "Ambiente"
        ];

        foreach (string column in expectedColumns)
        {
            Assert.Contains(column, migrationSource);
        }
    }

    [Fact]
    public void Migration_DoesNotModifyOtherTables()
    {
        string migrationSource = ReadMigrationFile();

        string[] existingTables =
        [
            "CategoriasEstablecimiento",
            "Establecimientos",
            "Sedes",
            "Restricciones",
            "Servicios",
            "SedesServicios",
            "SedesRestricciones"
        ];

        foreach (string table in existingTables)
        {
            Assert.DoesNotContain(table, migrationSource);
        }
    }

    [Fact]
    public void Migration_DoesNotAddEntityToSnapshot()
    {
        var snapshotType = InfrastructureAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "LyriaDbContextModelSnapshot");

        Assert.NotNull(snapshotType);

        // Read the snapshot source file to verify no LogsAplicacion entity was added
        string snapshotSource = ReadSnapshotFile();

        Assert.DoesNotContain("LogAplicacion", snapshotSource);
        Assert.DoesNotContain("LogsAplicacion", snapshotSource);
    }

    private static string ReadMigrationFile()
    {
        string sourceDir = FindSourceDirectory();
        string migrationPath = Path.Combine(sourceDir, "src", "Lyria.Infrastructure",
            "Persistence", "Migrations", "20260723000000_AddApplicationLogs.cs");

        Assert.True(File.Exists(migrationPath),
            $"No se encontró la migración en: {migrationPath}");

        return File.ReadAllText(migrationPath);
    }

    private static string ReadSnapshotFile()
    {
        string sourceDir = FindSourceDirectory();
        string snapshotPath = Path.Combine(sourceDir, "src", "Lyria.Infrastructure",
            "Persistence", "Migrations", "LyriaDbContextModelSnapshot.cs");

        Assert.True(File.Exists(snapshotPath),
            $"No se encontró el snapshot en: {snapshotPath}");

        return File.ReadAllText(snapshotPath);
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
