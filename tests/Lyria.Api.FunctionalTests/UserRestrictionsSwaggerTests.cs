using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class UserRestrictionsSwaggerTests
{
    private const string CollectionPath = "/api/v1/users/{userId}/restrictions";
    private const string ItemPath = "/api/v1/users/{userId}/restrictions/{restrictionId}";

    private readonly HttpClient _client;

    public UserRestrictionsSwaggerTests(WebApplicationFactory<Program> factory)
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
    public async Task SwaggerJson_ContainsCollectionEndpoints()
    {
        JsonElement paths = await GetPathsAsync();

        Assert.True(paths.TryGetProperty(CollectionPath, out JsonElement collection));
        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsItemEndpoints()
    {
        JsonElement paths = await GetPathsAsync();

        Assert.True(paths.TryGetProperty(ItemPath, out JsonElement item));
        Assert.True(item.TryGetProperty("get", out _));
        Assert.True(item.TryGetProperty("put", out _));
        Assert.True(item.TryGetProperty("delete", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsExactlyFiveUserRestrictionEndpoints()
    {
        JsonElement paths = await GetPathsAsync();

        int count = 0;

        if (paths.TryGetProperty(CollectionPath, out JsonElement collection))
        {
            count += collection.EnumerateObject().Count();
        }

        if (paths.TryGetProperty(ItemPath, out JsonElement item))
        {
            count += item.EnumerateObject().Count();
        }

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task SwaggerJson_EndpointsHaveSummaries()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement collection = paths.GetProperty(CollectionPath);
        JsonElement item = paths.GetProperty(ItemPath);

        foreach (JsonElement operation in new[]
        {
            collection.GetProperty("get"),
            collection.GetProperty("post"),
            item.GetProperty("get"),
            item.GetProperty("put"),
            item.GetProperty("delete")
        })
        {
            Assert.True(operation.TryGetProperty("summary", out JsonElement summary));
            Assert.False(string.IsNullOrWhiteSpace(summary.GetString()));
        }
    }

    [Fact]
    public async Task SwaggerJson_AssignEndpointDocumentsRequestBody()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement post = paths.GetProperty(CollectionPath).GetProperty("post");

        Assert.True(post.TryGetProperty("requestBody", out JsonElement requestBody));

        string requestJson = requestBody.GetRawText();
        Assert.Contains("AssignRestrictionToUserRequest", requestJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerJson_UpdateEndpointDocumentsRequestBody()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement put = paths.GetProperty(ItemPath).GetProperty("put");

        Assert.True(put.TryGetProperty("requestBody", out JsonElement requestBody));

        string requestJson = requestBody.GetRawText();
        Assert.Contains("UpdateUserRestrictionRequest", requestJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerJson_AssignEndpointDocumentsResponses()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement responses = paths.GetProperty(CollectionPath)
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("404", out _));
        Assert.True(responses.TryGetProperty("409", out _));
    }

    [Fact]
    public async Task SwaggerJson_ListEndpointDocumentsResponses()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement responses = paths.GetProperty(CollectionPath)
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("404", out _));
    }

    [Fact]
    public async Task SwaggerJson_ItemEndpointsDocumentResponses()
    {
        JsonElement paths = await GetPathsAsync();
        JsonElement item = paths.GetProperty(ItemPath);

        JsonElement getResponses = item.GetProperty("get").GetProperty("responses");
        Assert.True(getResponses.TryGetProperty("200", out _));
        Assert.True(getResponses.TryGetProperty("404", out _));

        JsonElement putResponses = item.GetProperty("put").GetProperty("responses");
        Assert.True(putResponses.TryGetProperty("204", out _));
        Assert.True(putResponses.TryGetProperty("400", out _));
        Assert.True(putResponses.TryGetProperty("404", out _));

        JsonElement deleteResponses = item.GetProperty("delete").GetProperty("responses");
        Assert.True(deleteResponses.TryGetProperty("204", out _));
        Assert.True(deleteResponses.TryGetProperty("404", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsUserRestrictionsTag()
    {
        JsonElement doc = await GetSwaggerDocAsync();

        var tagNames = new List<string>();
        foreach (JsonElement tag in doc.GetProperty("tags").EnumerateArray())
        {
            tagNames.Add(tag.GetProperty("name").GetString()!);
        }

        Assert.Contains("Restricciones de Usuario", tagNames);
    }

    [Fact]
    public async Task SwaggerJson_ContainsUserRestrictionResponseSchema()
    {
        JsonElement doc = await GetSwaggerDocAsync();

        JsonElement schemas = doc.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("UserRestrictionResponse", out JsonElement schema));

        JsonElement properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("userId", out _));
        Assert.True(properties.TryGetProperty("restrictionId", out _));
        Assert.True(properties.TryGetProperty("restrictionName", out _));
        Assert.True(properties.TryGetProperty("importanceLevel", out _));
        Assert.True(properties.TryGetProperty("createdAtUtc", out _));
    }

    private async Task<JsonElement> GetPathsAsync()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        return doc.GetProperty("paths");
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
