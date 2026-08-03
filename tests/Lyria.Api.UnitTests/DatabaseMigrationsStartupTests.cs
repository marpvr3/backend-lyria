using Lyria.Api.Extensions;
using Lyria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lyria.Api.UnitTests;

/// <summary>
/// Verifica la extensión de inicio que aplica migraciones y el binding de
/// <see cref="DatabaseStartupOptions"/>.
/// </summary>
public class DatabaseMigrationsStartupTests
{
    private const string SettingKey = "Database:ApplyMigrationsOnStartup";
    private const string EnvironmentVariableKey = "Database__ApplyMigrationsOnStartup";

    [Fact]
    public void DatabaseStartupOptions_DefaultValue_IsDisabled()
    {
        var options = new DatabaseStartupOptions();

        Assert.False(options.ApplyMigrationsOnStartup);
    }

    [Fact]
    public async Task ApplyPendingMigrations_WhenDisabled_DoesNotTouchTheDatabase()
    {
        // No se registra ningún DbContext: si el mecanismo intentara migrar,
        // la resolución del contexto fallaría.
        await using WebApplication app = BuildApp(applyMigrations: "false");

        WebApplication result = await app.ApplyPendingMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Same(app, result);
    }

    [Fact]
    public async Task ApplyPendingMigrations_WhenSettingIsAbsent_DoesNotTouchTheDatabase()
    {
        await using WebApplication app = BuildApp(applyMigrations: null);

        WebApplication result = await app.ApplyPendingMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Same(app, result);
    }

    [Fact]
    public async Task ApplyPendingMigrations_WhenEnabled_ResolvesDbContextAndPropagatesFailures()
    {
        await using WebApplication app = BuildApp(applyMigrations: "true");

        // Sin DbContext registrado, la resolución falla: el error se propaga
        // en lugar de degradarse a una advertencia.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.ApplyPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Configuration_EnvironmentVariable_OverridesFileValue()
    {
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableKey, "true");

            DatabaseStartupOptions options = BindOptions(fileValue: "false", readEnvironmentVariables: true);

            Assert.True(options.ApplyMigrationsOnStartup);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableKey, null);
        }
    }

    [Fact]
    public void Configuration_WithoutEnvironmentVariable_KeepsFileValue()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableKey, null);

        DatabaseStartupOptions options = BindOptions(fileValue: "false", readEnvironmentVariables: true);

        Assert.False(options.ApplyMigrationsOnStartup);
    }

    private static WebApplication BuildApp(string? applyMigrations)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [SettingKey] = applyMigrations
        });

        builder.Services.AddOptions<DatabaseStartupOptions>()
            .Bind(builder.Configuration.GetSection(DatabaseStartupOptions.SectionName));

        return builder.Build();
    }

    private static DatabaseStartupOptions BindOptions(string? fileValue, bool readEnvironmentVariables)
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SettingKey] = fileValue
            });

        if (readEnvironmentVariables)
        {
            configurationBuilder.AddEnvironmentVariables();
        }

        IConfiguration configuration = configurationBuilder.Build();

        var options = new DatabaseStartupOptions();
        configuration.GetSection(DatabaseStartupOptions.SectionName).Bind(options);

        return options;
    }
}
