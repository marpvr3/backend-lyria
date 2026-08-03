using System.Net;
using Lyria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Verifica que la API inicia con la aplicación automática de migraciones
/// deshabilitada (valor predeterminado) y que el resto de la aplicación no cambia.
/// </summary>
[Collection(LyriaApiTestGroup.Name)]
public class DatabaseMigrationsStartupTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseMigrationsStartupTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True",
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log")
                });
            });
        });
    }

    [Fact]
    public void Configuration_ApplyMigrationsOnStartup_IsDisabledByDefault()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? value = configuration["Database:ApplyMigrationsOnStartup"];

        Assert.Equal("false", value, ignoreCase: true);
    }

    [Fact]
    public void Options_ApplyMigrationsOnStartup_AreBoundAndDisabledByDefault()
    {
        DatabaseStartupOptions options = _factory.Services
            .GetRequiredService<IOptions<DatabaseStartupOptions>>()
            .Value;

        Assert.False(options.ApplyMigrationsOnStartup);
    }

    [Fact]
    public async Task Api_StartsSuccessfully_WithMigrationsDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExistingEndpoints_ContinueWorking_WithMigrationsDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/establishment-categories", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Swagger_ContinuesWorking_WithMigrationsDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Api_DoesNotExpose_MigrationEndpoint()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/migrations", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
