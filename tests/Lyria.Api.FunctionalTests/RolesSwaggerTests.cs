using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Verifica que el contrato OpenAPI del módulo de Roles no expone el campo Code.
/// </summary>
[Collection(LyriaApiTestGroup.Name)]
public class RolesSwaggerTests
{
    private const string CollectionPath = "/api/v1/roles";
    private const string ItemPath = "/api/v1/roles/{roleId}";
    private const string ActiveStatusPath = "/api/v1/roles/{roleId}/active-status";

    private readonly HttpClient _client;

    public RolesSwaggerTests(WebApplicationFactory<Program> factory)
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
    public async Task SwaggerJson_CreateRoleRequest_ContainsOnlyNameAndDescription()
    {
        JsonElement schemas = await GetSchemasAsync();

        Assert.True(schemas.TryGetProperty("CreateRoleRequest", out JsonElement schema));

        string[] properties = schema.GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["name", "description"], properties);
    }

    [Fact]
    public async Task SwaggerJson_UpdateRoleRequest_ContainsOnlyNameAndDescription()
    {
        JsonElement schemas = await GetSchemasAsync();

        Assert.True(schemas.TryGetProperty("UpdateRoleRequest", out JsonElement schema));

        string[] properties = schema.GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["name", "description"], properties);
    }

    [Fact]
    public async Task SwaggerJson_RoleResponse_DoesNotContainCode()
    {
        JsonElement schemas = await GetSchemasAsync();

        Assert.True(schemas.TryGetProperty("RoleResponse", out JsonElement schema));

        JsonElement properties = schema.GetProperty("properties");

        Assert.False(properties.TryGetProperty("code", out _));
        Assert.True(properties.TryGetProperty("id", out _));
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.True(properties.TryGetProperty("description", out _));
        Assert.True(properties.TryGetProperty("isActive", out _));
    }

    [Fact]
    public async Task SwaggerJson_RoleListItemResponse_DoesNotContainCode()
    {
        JsonElement schemas = await GetSchemasAsync();

        Assert.True(schemas.TryGetProperty("RoleListItemResponse", out JsonElement schema));

        string[] properties = schema.GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["id", "name", "isActive"], properties);
    }

    [Fact]
    public async Task SwaggerJson_UserRoleResponse_DoesNotContainRoleCode()
    {
        JsonElement schemas = await GetSchemasAsync();

        Assert.True(schemas.TryGetProperty("UserRoleResponse", out JsonElement schema));

        JsonElement properties = schema.GetProperty("properties");

        Assert.False(properties.TryGetProperty("roleCode", out _));
        Assert.True(properties.TryGetProperty("roleId", out _));
        Assert.True(properties.TryGetProperty("roleName", out _));
    }

    [Fact]
    public async Task SwaggerJson_ListRolesEndpoint_HasNoCodeQueryParameter()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement get = paths.GetProperty(CollectionPath).GetProperty("get");

        string[] parameterNames = get.GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()!)
            .ToArray();

        Assert.DoesNotContain("code", parameterNames);
        Assert.Contains("search", parameterNames);
        Assert.Contains("isActive", parameterNames);
    }

    [Fact]
    public async Task SwaggerJson_CreateRoleEndpoint_DoesNotDocumentConflict()
    {
        JsonElement paths = await GetPathsAsync();

        JsonElement responses = paths.GetProperty(CollectionPath)
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.False(responses.TryGetProperty("409", out _));
    }

    [Fact]
    public async Task SwaggerJson_RoleEndpointsRemainUnchanged()
    {
        JsonElement paths = await GetPathsAsync();

        Assert.True(paths.TryGetProperty(CollectionPath, out JsonElement collection));
        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));

        Assert.True(paths.TryGetProperty(ItemPath, out JsonElement item));
        Assert.True(item.TryGetProperty("get", out _));
        Assert.True(item.TryGetProperty("put", out _));

        Assert.True(paths.TryGetProperty(ActiveStatusPath, out JsonElement activeStatus));
        Assert.True(activeStatus.TryGetProperty("patch", out _));
    }

    [Fact]
    public async Task SwaggerJson_RoleSchemas_ContainNoResidualCodeReference()
    {
        JsonElement schemas = await GetSchemasAsync();

        foreach (string schemaName in new[]
        {
            "CreateRoleRequest",
            "UpdateRoleRequest",
            "RoleResponse",
            "RoleListItemResponse",
            "UserRoleResponse"
        })
        {
            string raw = schemas.GetProperty(schemaName).GetRawText();

            Assert.DoesNotContain("code", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("codigo", raw, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<JsonElement> GetPathsAsync()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        return doc.GetProperty("paths");
    }

    private async Task<JsonElement> GetSchemasAsync()
    {
        JsonElement doc = await GetSwaggerDocAsync();
        return doc.GetProperty("components").GetProperty("schemas");
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
