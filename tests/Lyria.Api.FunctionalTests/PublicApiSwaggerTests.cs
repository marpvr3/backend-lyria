using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class PublicApiSwaggerTests
{
    private readonly HttpClient _client;

    public PublicApiSwaggerTests(WebApplicationFactory<Program> factory)
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
    public async Task SwaggerJson_ContainsPublicEstablishmentsEndpoints()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/public/establishments", out JsonElement establishments));
        Assert.True(establishments.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsPublicEstablishmentBySlugEndpoint()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/public/establishments/{slug}", out JsonElement bySlug));
        Assert.True(bySlug.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsPublicBranchesEndpoint()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/public/branches/{branchId}", out JsonElement byId));
        Assert.True(byId.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsPublicCatalogsEndpoint()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/public/catalogs", out JsonElement catalogs));
        Assert.True(catalogs.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_PublicEndpointsAreGetOnly()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        string[] publicPaths =
        [
            "/api/v1/public/establishments",
            "/api/v1/public/establishments/{slug}",
            "/api/v1/public/branches/{branchId}",
            "/api/v1/public/catalogs"
        ];

        string[] nonGetMethods = ["post", "put", "patch", "delete"];

        foreach (string path in publicPaths)
        {
            if (paths.TryGetProperty(path, out JsonElement endpoint))
            {
                foreach (string method in nonGetMethods)
                {
                    Assert.False(endpoint.TryGetProperty(method, out _),
                        $"El endpoint público {path} no debe tener método {method.ToUpperInvariant()}.");
                }
            }
        }
    }

    private async Task<JsonElement> GetSwaggerDocAsync()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
    }
}
