using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class MobileRegistrationsSwaggerTests
{
    private const string EndpointPath = "/api/v1/mobile/registrations";
    private const string RequestSchemaName = "MobileRegistrationRequest";
    private const string ResponseSchemaName = "MobileRegistrationResponse";

    private readonly HttpClient _client;

    public MobileRegistrationsSwaggerTests(WebApplicationFactory<Program> factory)
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

    private async Task<JsonElement> GetDocumentAsync()
    {
        return await _client.GetFromJsonAsync<JsonElement>(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
    }

    private async Task<JsonElement> GetSchemaAsync(string schemaName)
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement schemas = document
            .GetProperty("components")
            .GetProperty("schemas");

        Assert.True(schemas.TryGetProperty(schemaName, out JsonElement schema));

        return schema;
    }

    private static IReadOnlyList<string> PropertyNames(JsonElement schema) =>
        [.. schema.GetProperty("properties").EnumerateObject().Select(p => p.Name)];

    [Fact]
    public async Task SwaggerJson_DocumentsTheEndpoint()
    {
        JsonElement document = await GetDocumentAsync();

        Assert.True(document.GetProperty("paths")
            .TryGetProperty(EndpointPath, out JsonElement path));
        Assert.True(path.TryGetProperty("post", out _));
    }

    [Fact]
    public async Task SwaggerJson_ExposesOnlyPostOnTheEndpoint()
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement path = document.GetProperty("paths").GetProperty(EndpointPath);

        Assert.Equal("post", Assert.Single(path.EnumerateObject()).Name);
    }

    [Fact]
    public async Task SwaggerJson_DocumentsTheExpectedStatusCodes()
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement responses = document
            .GetProperty("paths")
            .GetProperty(EndpointPath)
            .GetProperty("post")
            .GetProperty("responses");

        foreach (string statusCode in new[] { "201", "400", "404", "409", "500" })
        {
            Assert.True(
                responses.TryGetProperty(statusCode, out _),
                $"Falta el código de respuesta {statusCode}.");
        }
    }

    [Theory]
    [InlineData("roleId")]
    [InlineData("roleName")]
    [InlineData("roleCode")]
    [InlineData("passwordHash")]
    [InlineData("importanceLevel")]
    [InlineData("status")]
    [InlineData("isEmailVerified")]
    [InlineData("scopeType")]
    [InlineData("establishmentId")]
    [InlineData("branchId")]
    [InlineData("isAdmin")]
    public async Task SwaggerJson_RequestDoesNotExposeBackendControlledField(string fieldName)
    {
        JsonElement schema = await GetSchemaAsync(RequestSchemaName);

        Assert.DoesNotContain(
            fieldName, PropertyNames(schema), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwaggerJson_RequestExposesOnlyTheExpectedFields()
    {
        JsonElement schema = await GetSchemaAsync(RequestSchemaName);

        Assert.Equal(
            [
                "birthDate", "email", "lastName", "name",
                "password", "phone", "photoUrl", "restrictionIds"
            ],
            PropertyNames(schema).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task SwaggerJson_ResponseDoesNotExposePasswordOrRole()
    {
        JsonElement schema = await GetSchemaAsync(ResponseSchemaName);
        IReadOnlyList<string> properties = PropertyNames(schema);

        Assert.DoesNotContain("password", properties, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", properties, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("roleId", properties, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwaggerJson_ResponseExposesOnlyTheExpectedFields()
    {
        JsonElement schema = await GetSchemaAsync(ResponseSchemaName);

        Assert.Equal(
            ["email", "lastName", "name", "restrictionIds", "status", "userId"],
            PropertyNames(schema).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task SwaggerJson_KeepsAdministrativeUserEndpointsUnchanged()
    {
        JsonElement paths = (await GetDocumentAsync()).GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/users", out JsonElement users));
        Assert.True(users.TryGetProperty("post", out _));
        Assert.True(users.TryGetProperty("get", out _));

        JsonElement createUserSchema = await GetSchemaAsync("CreateUserRequest");

        Assert.Equal(
            ["birthDate", "email", "lastName", "name", "password", "phone", "photoUrl"],
            PropertyNames(createUserSchema).Order(StringComparer.Ordinal));
    }
}
