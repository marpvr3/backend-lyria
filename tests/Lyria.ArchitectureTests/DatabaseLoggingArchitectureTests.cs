using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace Lyria.ArchitectureTests;

public class DatabaseLoggingArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Program).Assembly;

    [Fact]
    public void SerilogSinksMSSqlServer_OnlyInApiProject()
    {
        var apiReferences = ApiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.Contains(apiReferences, name =>
            name!.Contains("Serilog.Sinks.MSSqlServer", StringComparison.OrdinalIgnoreCase));

        AssertAssemblyDoesNotReference(DomainAssembly, "Serilog.Sinks.MSSqlServer");
        AssertAssemblyDoesNotReference(ApplicationAssembly, "Serilog.Sinks.MSSqlServer");
        AssertAssemblyDoesNotReference(InfrastructureAssembly, "Serilog.Sinks.MSSqlServer");
    }

    [Fact]
    public void Domain_DoesNotReference_Serilog()
    {
        var references = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_DoesNotReference_Serilog()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Infrastructure_DoesNotReference_SerilogSinksMSSqlServer()
    {
        AssertAssemblyDoesNotReference(InfrastructureAssembly, "Serilog.Sinks.MSSqlServer");
    }

    [Fact]
    public void Domain_DoesNotContain_LogAplicacionEntity()
    {
        var domainTypes = DomainAssembly.GetTypes();

        Assert.DoesNotContain(domainTypes, t =>
            t.Name.Contains("LogAplicacion", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("ApplicationLog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NoDbSet_ForLogs()
    {
        var dbContextTypes = InfrastructureAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("DbContext", StringComparison.Ordinal))
            .ToList();

        foreach (var dbContextType in dbContextTypes)
        {
            var properties = dbContextType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.DoesNotContain(properties, p =>
                p.Name.Contains("Log", StringComparison.OrdinalIgnoreCase) &&
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void NoController_ForLogs()
    {
        var controllers = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain(controllers, c =>
            c.Name.Contains("Log", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NoRepository_ForLogs()
    {
        var allTypes = DomainAssembly.GetTypes()
            .Concat(ApplicationAssembly.GetTypes())
            .Concat(InfrastructureAssembly.GetTypes())
            .ToList();

        Assert.DoesNotContain(allTypes, t =>
            t.Name.Contains("LogRepository", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("LogsRepository", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AutoCreateSqlTable_IsNotEnabled()
    {
        string sourceDir = FindSourceDirectory();
        string extensionsFile = Path.Combine(sourceDir, "src", "Lyria.Api", "Extensions",
            "SerilogDatabaseLoggingExtensions.cs");

        Assert.True(File.Exists(extensionsFile));

        string content = File.ReadAllText(extensionsFile);

        Assert.Contains("AutoCreateSqlTable = false", content);
    }

    [Fact]
    public void AutoCreateSqlDatabase_IsNotEnabled()
    {
        string sourceDir = FindSourceDirectory();
        string extensionsFile = Path.Combine(sourceDir, "src", "Lyria.Api", "Extensions",
            "SerilogDatabaseLoggingExtensions.cs");

        Assert.True(File.Exists(extensionsFile));

        string content = File.ReadAllText(extensionsFile);

        Assert.Contains("AutoCreateSqlDatabase = false", content);
    }

    private static void AssertAssemblyDoesNotReference(Assembly assembly, string packageName)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(references, name =>
            name!.Contains(packageName, StringComparison.OrdinalIgnoreCase));
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
