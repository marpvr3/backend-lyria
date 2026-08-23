using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class EmailVerificationSwaggerTests : IDisposable
{
    private const string ResendPath = "/api/v1/auth/email-verification/resend";
    private const string ConfirmPath = "/api/v1/auth/email-verification/confirm";

    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _configured;

    /// <summary>
    /// El host derivado se libera al terminar cada prueba para no acumularlos durante
    /// toda la ejecución del proyecto.
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
        _configured.Dispose();
        GC.SuppressFinalize(this);
    }

    public EmailVerificationSwaggerTests(WebApplicationFactory<Program> factory)
    {
        _configured = factory.WithWebHostBuilder(builder =>
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

        _client = _configured.CreateClient();
    }

    private async Task<JsonElement> GetDocumentAsync() =>
        await _client.GetFromJsonAsync<JsonElement>(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

    private static JsonElement GetOperation(
        JsonElement document, string path, string method) =>
        document.GetProperty("paths").GetProperty(path).GetProperty(method);

    // --- Endpoints documentados ---

    [Theory]
    [InlineData(ResendPath)]
    [InlineData(ConfirmPath)]
    public async Task Swagger_DocumentsTheEmailVerificationEndpoints(string path)
    {
        JsonElement document = await GetDocumentAsync();

        Assert.True(document.GetProperty("paths").TryGetProperty(path, out JsonElement item));
        Assert.True(item.TryGetProperty("post", out _));
    }

    /// <summary>
    /// No se creó ningún endpoint adicional de verificación: el envío inicial forma parte
    /// del registro móvil.
    /// </summary>
    [Fact]
    public async Task Swagger_DocumentsExactlyTwoEmailVerificationEndpoints()
    {
        JsonElement document = await GetDocumentAsync();

        string[] paths = [.. document.GetProperty("paths")
            .EnumerateObject()
            .Select(p => p.Name)
            .Where(name => name.Contains("email-verification", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Equal([ConfirmPath, ResendPath], paths);
    }

    // --- Códigos de respuesta ---

    [Theory]
    [InlineData("202")]
    [InlineData("400")]
    [InlineData("429")]
    [InlineData("500")]
    public async Task Swagger_DocumentsTheResendResponses(string statusCode)
    {
        JsonElement responses = GetOperation(await GetDocumentAsync(), ResendPath, "post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _));
    }

    [Theory]
    [InlineData("204")]
    [InlineData("400")]
    [InlineData("429")]
    [InlineData("500")]
    public async Task Swagger_DocumentsTheConfirmResponses(string statusCode)
    {
        JsonElement responses = GetOperation(await GetDocumentAsync(), ConfirmPath, "post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _));
    }

    // --- Anonimato ---

    [Theory]
    [InlineData(ResendPath)]
    [InlineData(ConfirmPath)]
    public async Task Swagger_DoesNotRequireBearerForTheseEndpoints(string path)
    {
        JsonElement operation = GetOperation(await GetDocumentAsync(), path, "post");

        Assert.False(operation.TryGetProperty("security", out _));
    }

    // --- Esquemas ---

    /// <summary>
    /// El request de confirmación sí muestra el código: es un dato que el usuario
    /// introduce.
    /// </summary>
    [Fact]
    public async Task Swagger_ConfirmRequest_ExposesEmailAndCode()
    {
        JsonElement schema = (await GetDocumentAsync())
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ConfirmEmailVerificationRequest")
            .GetProperty("properties");

        Assert.True(schema.TryGetProperty("email", out _));
        Assert.True(schema.TryGetProperty("code", out _));
    }

    /// <summary>
    /// Ni la entidad de dominio ni ningún dato sensible pueden aparecer en el contrato.
    /// </summary>
    [Fact]
    public async Task Swagger_DoesNotExposeInternalModelsOrSecrets()
    {
        string document = await _client.GetStringAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        foreach (string forbidden in new[]
        {
            "UserEmailVerification",
            "codeHash",
            "codigoHash",
            "codeSecret",
            "smtpHost",
            "smtpPassword",
            "EmailOptions",
            "EmailVerificationOptions"
        })
        {
            Assert.DoesNotContain(forbidden, document, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Swagger_GroupsTheEndpointsUnderTheirOwnTag()
    {
        JsonElement operation = GetOperation(await GetDocumentAsync(), ResendPath, "post");

        Assert.Equal(
            "Verificación de Correo",
            operation.GetProperty("tags").EnumerateArray().Single().GetString());
    }
}
