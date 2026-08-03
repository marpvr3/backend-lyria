using System.Text.Json;
using Xunit;

namespace Lyria.ArchitectureTests;

/// <summary>
/// Reglas de seguridad del mecanismo de migraciones automáticas: el código
/// productivo solo puede modificar el esquema a través de migraciones EF Core,
/// y la opción que las habilita debe permanecer deshabilitada por defecto.
/// </summary>
public class DatabaseMigrationArchitectureTests
{
    private static readonly string[] ForbiddenSchemaApis =
    [
        "EnsureCreated",
        "EnsureCreatedAsync",
        "EnsureDeleted",
        "EnsureDeletedAsync",
        "ExecuteSqlRaw",
        "ExecuteSqlRawAsync",
        "ExecuteSqlInterpolated",
        "ExecuteSqlInterpolatedAsync"
    ];

    [Fact]
    public void ProductionCode_DoesNotUse_SchemaCreationApis()
    {
        foreach (string file in GetProductionSourceFiles())
        {
            string source = File.ReadAllText(file);

            foreach (string api in ForbiddenSchemaApis)
            {
                Assert.DoesNotContain(api, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void ProductionCode_UsesOnly_EfCoreMigrationApis()
    {
        string migratorSource = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Infrastructure", "Persistence", "DatabaseMigrator.cs"));

        Assert.Contains("GetPendingMigrationsAsync", migratorSource, StringComparison.Ordinal);
        Assert.Contains("MigrateAsync", migratorSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrator_IsInvokedOnlyFrom_StartupExtension()
    {
        List<string> callers = GetProductionSourceFiles()
            .Where(file => File.ReadAllText(file)
                .Contains("DatabaseMigrator.ApplyPendingMigrationsAsync", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["WebApplicationExtensions.cs"], callers);
    }

    [Fact]
    public void Controllers_DoNotApplyMigrations()
    {
        IEnumerable<string> controllers = Directory.EnumerateFiles(
            Path.Combine(SourceRoot, "src", "Lyria.Api", "Controllers"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (string controller in controllers)
        {
            string source = File.ReadAllText(controller);

            Assert.DoesNotContain("Migrate", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DatabaseMigrator", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Startup_AppliesMigrations_BeforeConfiguringThePipeline()
    {
        string program = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Api", "Program.cs"));

        int migrationsIndex = program.IndexOf(
            "ApplyPendingMigrationsAsync", StringComparison.Ordinal);
        int pipelineIndex = program.IndexOf(
            "UseLyriaPipeline", StringComparison.Ordinal);

        Assert.True(migrationsIndex >= 0, "Program.cs no aplica las migraciones pendientes.");
        Assert.True(pipelineIndex >= 0, "Program.cs no configura el pipeline.");
        Assert.True(
            migrationsIndex < pipelineIndex,
            "Las migraciones deben aplicarse antes de configurar el pipeline HTTP.");
    }

    [Fact]
    public void Startup_Rethrows_StartupFailures()
    {
        string program = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Api", "Program.cs"));

        int catchIndex = program.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
        int finallyIndex = program.IndexOf("finally", StringComparison.Ordinal);

        Assert.True(catchIndex >= 0, "Program.cs no captura las excepciones de arranque.");
        Assert.True(finallyIndex > catchIndex, "Program.cs no cierra Serilog en un bloque finally.");

        string catchBlock = program[catchIndex..finallyIndex];

        Assert.Contains("Log.Fatal", catchBlock, StringComparison.Ordinal);
        Assert.Contains("throw;", catchBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_ClosesSerilog_OnFailure()
    {
        string program = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Api", "Program.cs"));

        int finallyIndex = program.IndexOf("finally", StringComparison.Ordinal);

        Assert.True(finallyIndex >= 0, "Program.cs no cierra Serilog en un bloque finally.");
        Assert.Contains("Log.CloseAndFlush", program[finallyIndex..], StringComparison.Ordinal);
    }

    [Fact]
    public void AppSettings_ApplyMigrationsOnStartup_IsDisabledByDefault()
    {
        string appSettings = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Api", "appsettings.json"));

        using JsonDocument document = JsonDocument.Parse(appSettings);

        JsonElement database = document.RootElement.GetProperty("Database");
        JsonElement applyMigrations = database.GetProperty("ApplyMigrationsOnStartup");

        Assert.Equal(JsonValueKind.False, applyMigrations.ValueKind);
    }

    [Fact]
    public void DevelopmentSettings_DoNotEnable_ApplyMigrationsOnStartup()
    {
        string appSettings = File.ReadAllText(Path.Combine(
            SourceRoot, "src", "Lyria.Api", "appsettings.Development.json"));

        using JsonDocument document = JsonDocument.Parse(appSettings);

        Assert.False(
            document.RootElement.TryGetProperty("Database", out JsonElement database) &&
            database.TryGetProperty("ApplyMigrationsOnStartup", out JsonElement applyMigrations) &&
            applyMigrations.ValueKind == JsonValueKind.True,
            "Ningún archivo de configuración debe habilitar las migraciones automáticas.");
    }

    private static IEnumerable<string> GetProductionSourceFiles()
    {
        string sourceDirectory = Path.Combine(SourceRoot, "src");

        return Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            // Las migraciones generadas por EF Core contienen SQL propio de cada
            // cambio de esquema y se revisan por separado.
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal));
    }

    private static string SourceRoot => FindSourceDirectory();

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
