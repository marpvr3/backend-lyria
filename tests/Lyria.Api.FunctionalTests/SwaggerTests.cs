using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

public class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        WebApplicationFactory<Program> configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True"
                });
            });
        });

        _client = configuredFactory.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_IsServed()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SwaggerJson_ContainsApiInfo()
    {
        JsonElement doc = await GetSwaggerDocAsync();

        JsonElement info = doc.GetProperty("info");
        Assert.Equal("Lyria API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.False(string.IsNullOrWhiteSpace(info.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task SwaggerJson_ContainsEstablishmentsEndpoints()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/establishments", out JsonElement establishments));
        Assert.True(establishments.TryGetProperty("get", out _));
        Assert.True(establishments.TryGetProperty("post", out _));

        Assert.True(paths.TryGetProperty("/api/v1/establishments/{id}", out JsonElement byId));
        Assert.True(byId.TryGetProperty("get", out _));
        Assert.True(byId.TryGetProperty("put", out _));

        Assert.True(paths.TryGetProperty("/api/v1/establishments/{id}/status", out JsonElement status));
        Assert.True(status.TryGetProperty("patch", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsCategoryEndpoints()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/establishment-categories", out JsonElement categories));
        Assert.True(categories.TryGetProperty("get", out _));

        Assert.True(paths.TryGetProperty("/api/v1/establishment-categories/{id}", out JsonElement byId));
        Assert.True(byId.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsHealthEndpoint()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/health", out JsonElement health));
        Assert.True(health.TryGetProperty("get", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsSpanishTags()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement tags = doc.GetProperty("tags");

        var tagNames = new List<string>();
        foreach (JsonElement tag in tags.EnumerateArray())
        {
            tagNames.Add(tag.GetProperty("name").GetString()!);
        }

        Assert.Contains("Establecimientos", tagNames);
        Assert.Contains("Categorías de establecimientos", tagNames);
        Assert.Contains("Salud", tagNames);
    }

    [Fact]
    public async Task SwaggerJson_EndpointsHaveDescriptions()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        JsonElement paths = doc.GetProperty("paths");

        JsonElement getEstablishments = paths
            .GetProperty("/api/v1/establishments")
            .GetProperty("get");

        Assert.True(getEstablishments.TryGetProperty("summary", out JsonElement summary));
        Assert.False(string.IsNullOrWhiteSpace(summary.GetString()));
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/index.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
