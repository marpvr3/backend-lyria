using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class SerilogPipelineTests
{
    private readonly HttpClient _client;

    public SerilogPipelineTests(WebApplicationFactory<Program> factory)
    {
        WebApplicationFactory<Program> configuredFactory = factory.WithWebHostBuilder(builder =>
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

        _client = configuredFactory.CreateClient();
    }

    [Fact]
    public async Task RequestLogging_DoesNotBreak_HealthEndpoint()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestLogging_DoesNotBreak_SwaggerEndpoint()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestLogging_DoesNotBreak_ApiEndpoints()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/api/v1/establishment-categories", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RequestLogging_Returns404_ForNonExistentRoute()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/api/v1/non-existent-endpoint", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
