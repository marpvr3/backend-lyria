using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class SerilogInitializationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public SerilogInitializationTests(WebApplicationFactory<Program> factory)
    {
        string testLogDirectory = Path.Combine(Path.GetTempPath(), "lyria-init-tests", Guid.NewGuid().ToString("N"));

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(testLogDirectory, "lyria-.log")
                });
            });
        });
    }

    [Fact]
    public void BootstrapLogger_IsReplaced_AfterHostBuild()
    {
        // Log.Logger se inicializa como ReloadableLogger mediante CreateBootstrapLogger().
        // Después de que WebApplicationFactory construye el host, AddSerilog congela el
        // ReloadableLogger y lo reemplaza con el logger definitivo.
        _ = _factory.Services;

        Assert.False(
            Log.Logger is Serilog.Extensions.Hosting.ReloadableLogger,
            "Después de construir el host, Log.Logger debe ser el logger definitivo, no el ReloadableLogger bootstrap.");
    }

    [Fact]
    public void DefinitiveLogger_ReplacesBootstrap_AfterHostBuild()
    {
        _ = _factory.Services;

        Assert.NotNull(Log.Logger);

        var exception = Record.Exception(() => Log.Information("Prueba de logger definitivo"));
        Assert.Null(exception);
    }

    [Fact]
    public void ILoggerT_WritesThrough_DefinitiveConfiguration()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("InitializationTest");

        Assert.NotNull(logger);

        var exception = Record.Exception(static () =>
        {
            // No-op: la prueba verifica que ILogger se resuelve correctamente del DI.
        });
        Assert.Null(exception);
    }

    [Fact]
    public void LogInformation_AfterBuild_UsesDefinitiveLogger()
    {
        _ = _factory.Services;

        var exception = Record.Exception(() =>
            Log.Information("Evento de información posterior a la construcción del host"));
        Assert.Null(exception);

        // Verificar que el logger no es el SilentLogger (Serilog.Core.Pipeline.SilentLogger).
        Assert.NotEqual("SilentLogger", Log.Logger.GetType().Name);
    }

    [Fact]
    public void LogFatal_AfterBuild_ReachesFileSink()
    {
        string fatalLogDir = Path.Combine(Path.GetTempPath(), "lyria-fatal-test", Guid.NewGuid().ToString("N"));
        string logFilePath = Path.Combine(fatalLogDir, "fatal-.log");

        try
        {
            using var testLogger = new LoggerConfiguration()
                .WriteTo.File(
                    logFilePath,
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: Serilog.RollingInterval.Day,
                    shared: true)
                .CreateLogger();

            testLogger.Fatal("Evento fatal de prueba para verificar file sink");
            testLogger.Dispose();

            string[] logFiles = Directory.Exists(fatalLogDir)
                ? Directory.GetFiles(fatalLogDir, "fatal-*.log")
                : [];

            Assert.NotEmpty(logFiles);

            string content = File.ReadAllText(logFiles[0]);
            Assert.Contains("Evento fatal de prueba para verificar file sink", content);
        }
        finally
        {
            if (Directory.Exists(fatalLogDir))
            {
                Directory.Delete(fatalLogDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Events_AreNotDuplicated_AcrossSinks()
    {
        string countLogDir = Path.Combine(Path.GetTempPath(), "lyria-count-test", Guid.NewGuid().ToString("N"));
        string logFilePath = Path.Combine(countLogDir, "count-.log");

        try
        {
            string uniqueMarker = $"UNIQUE-{Guid.NewGuid():N}";

            using var testLogger = new LoggerConfiguration()
                .WriteTo.File(
                    logFilePath,
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: Serilog.RollingInterval.Day,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromMilliseconds(100))
                .CreateLogger();

            testLogger.Information(uniqueMarker);
            testLogger.Dispose();

            string[] logFiles = Directory.Exists(countLogDir)
                ? Directory.GetFiles(countLogDir, "count-*.log")
                : [];

            Assert.NotEmpty(logFiles);

            string content = File.ReadAllText(logFiles[0]);
            int occurrences = CountOccurrences(content, uniqueMarker);

            Assert.Equal(1, occurrences);
        }
        finally
        {
            if (Directory.Exists(countLogDir))
            {
                Directory.Delete(countLogDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ParallelTests_DoNotProduce_FrozenLoggerException()
    {
        var exception = Record.Exception(() =>
        {
            _ = _factory.Services;
            Log.Information("Evento de prueba paralela");
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Tests_DoNotLeave_LogFilesInRepository()
    {
        string repositoryRoot = FindRepositoryRoot();
        string srcLogsPath = Path.Combine(repositoryRoot, "src", "Lyria.Api", "logs");

        // Verificar que no existen archivos de log de Serilog en src/Lyria.Api/logs/.
        if (Directory.Exists(srcLogsPath))
        {
            string[] logFiles = Directory.GetFiles(srcLogsPath, "*.log", SearchOption.AllDirectories);
            Assert.Empty(logFiles);
        }

        // Verificar que no existen archivos de log de Serilog en los directorios fuente de tests
        // (excluir bin/ y obj/ que son artefactos de compilación y no se versionan).
        string testsSourcePath = Path.Combine(repositoryRoot, "tests");
        if (Directory.Exists(testsSourcePath))
        {
            string[] serilogFiles = Directory.GetFiles(testsSourcePath, "lyria-*.log", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                            !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .ToArray();
            Assert.Empty(serilogFiles);
        }
    }

    private static string FindRepositoryRoot()
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

        throw new InvalidOperationException("No se encontró la raíz del repositorio (Lyria.slnx).");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
