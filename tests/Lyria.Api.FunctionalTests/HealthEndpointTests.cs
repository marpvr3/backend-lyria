using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class HealthEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
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
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log")
                });
            });
        });
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
