using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class DatabaseLoggingDisabledTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseLoggingDisabledTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True",
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log"),
                    ["DatabaseLogging:Enabled"] = "false"
                });
            });
        });
    }

    [Fact]
    public async Task Api_StartsSuccessfully_WithDatabaseLoggingDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExistingEndpoints_ContinueWorking_WithDatabaseLoggingDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/establishment-categories", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Swagger_ContinuesWorking_WithDatabaseLoggingDisabled()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Configuration_HasDatabaseLogging_Disabled()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? enabled = configuration["DatabaseLogging:Enabled"];

        Assert.Equal("false", enabled, ignoreCase: true);
    }

    [Fact]
    public void ConsoleSink_StillConfigured()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? consoleSink = configuration["Serilog:WriteTo:0:Name"];

        Assert.Equal("Console", consoleSink);
    }

    [Fact]
    public void FileSink_StillConfigured()
    {
        IConfiguration configuration = _factory.Services.GetRequiredService<IConfiguration>();

        string? fileSink = configuration["Serilog:WriteTo:1:Name"];

        Assert.Equal("File", fileSink);
    }
}
