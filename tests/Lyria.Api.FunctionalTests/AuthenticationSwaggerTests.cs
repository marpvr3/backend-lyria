using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class AuthenticationSwaggerTests
{
    private const string LoginPath = "/api/v1/auth/login";
    private const string RefreshPath = "/api/v1/auth/refresh";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string MePath = "/api/v1/users/me";
    private const string MobileRegistrationPath = "/api/v1/mobile/registrations";

    private readonly HttpClient _client;

    public AuthenticationSwaggerTests(WebApplicationFactory<Program> factory)
    {
        WebApplicationFactory<Program> configured = factory.WithWebHostBuilder(builder =>
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

        _client = configured.CreateClient();
    }

    private async Task<JsonElement> GetDocumentAsync() =>
        await _client.GetFromJsonAsync<JsonElement>(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

    private static JsonElement GetOperation(
        JsonElement document, string path, string method) =>
        document.GetProperty("paths").GetProperty(path).GetProperty(method);

    // --- Endpoints documentados ---

    [Theory]
    [InlineData(LoginPath, "post")]
    [InlineData(RefreshPath, "post")]
    [InlineData(LogoutPath, "post")]
    [InlineData(MePath, "get")]
    public async Task Swagger_DocumentsTheAuthenticationEndpoints(string path, string method)
    {
        JsonElement document = await GetDocumentAsync();

        Assert.True(document.GetProperty("paths").TryGetProperty(path, out JsonElement item));
        Assert.True(item.TryGetProperty(method, out _));
    }

    // --- Esquema Bearer ---

    [Fact]
    public async Task Swagger_DeclaresTheBearerSecurityScheme()
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement scheme = document
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task Swagger_MarksTheProfileEndpointAsProtected()
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement operation = GetOperation(document, MePath, "get");

        Assert.True(operation.TryGetProperty("security", out JsonElement security));
        Assert.NotEmpty(security.EnumerateArray());
    }

    /// <summary>
    /// Los endpoints anónimos no deben aparecer como protegidos: no hay requisito
    /// global de seguridad.
    /// </summary>
    [Theory]
    [InlineData(LoginPath, "post")]
    [InlineData(RefreshPath, "post")]
    [InlineData(LogoutPath, "post")]
    [InlineData(MobileRegistrationPath, "post")]
    public async Task Swagger_DoesNotMarkAnonymousEndpointsAsProtected(
        string path, string method)
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement operation = GetOperation(document, path, method);

        if (operation.TryGetProperty("security", out JsonElement security))
        {
            Assert.Empty(security.EnumerateArray());
        }
    }

    // --- Sin datos sensibles ---

    [Fact]
    public async Task Swagger_DoesNotExposeSensitiveFieldsInAnySchema()
    {
        JsonElement document = await GetDocumentAsync();

        JsonElement schemas = document.GetProperty("components").GetProperty("schemas");

        foreach (string schemaName in new[]
        {
            "LoginRequest", "RefreshRequest", "LogoutRequest",
            "AuthenticationResponse", "AuthenticatedUserResponse", "CurrentUserResponse"
        })
        {
            Assert.True(
                schemas.TryGetProperty(schemaName, out JsonElement schema),
                $"Falta el esquema {schemaName} en el documento OpenAPI.");

            string[] properties = [.. schema.GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)];

            foreach (string forbidden in new[]
            {
                "passwordHash", "tokenHash", "refreshTokenHash", "signingKey"
            })
            {
                Assert.DoesNotContain(forbidden, properties, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Swagger_DocumentDoesNotContainAnyRealTokenOrKey()
    {
        string document = await _client.GetStringAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Ningún JWT de ejemplo ni clave de firma incrustada.
        Assert.DoesNotContain("eyJ", document, StringComparison.Ordinal);
        Assert.DoesNotContain("Jwt__SigningKey", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerUi_IsStillReachableWithoutAuthentication()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/index.html", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_IsStillReachableWithoutAuthentication()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
