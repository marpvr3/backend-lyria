using System.Net;
using Lyria.Api.Extensions;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Valida la política CORS de Lyria: solo se autorizan los orígenes declarados
/// explícitamente en la configuración "Cors:AllowedOrigins".
/// </summary>
[Collection(LyriaApiTestGroup.Name)]
public class CorsConfigurationTests
{
    private const string AllowedOrigin = "http://localhost:5173";
    private const string DisallowedOrigin = "http://evil.example.com";
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string AllowMethodsHeader = "Access-Control-Allow-Methods";
    private const string AllowHeadersHeader = "Access-Control-Allow-Headers";
    private const string RestrictionsEndpoint =
        "/api/v1/restrictions?isActive=true&page=1&pageSize=20";

    private readonly WebApplicationFactory<Program> _factory;

    public CorsConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(BaseSettings(
                    new Dictionary<string, string?>
                    {
                        ["Cors:AllowedOrigins:0"] = AllowedOrigin
                    }));
            });

            // El endpoint afectado no debe depender de una base de datos real
            // para poder validar únicamente las cabeceras CORS.
            builder.ConfigureServices(services =>
                services.AddSingleton<IRestrictionReadService, StubRestrictionReadService>());
        });
    }

    private sealed class StubRestrictionReadService : IRestrictionReadService
    {
        public Task<RestrictionResponse?> GetByIdAsync(
            RestrictionId id,
            CancellationToken cancellationToken)
            => Task.FromResult<RestrictionResponse?>(null);

        public Task<PagedResponse<RestrictionListItemResponse>> ListAsync(
            RestrictionListFilter filter,
            CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<RestrictionListItemResponse>(
                [], filter.Page, filter.PageSize, 0));
    }

    private static Dictionary<string, string?> BaseSettings(
        Dictionary<string, string?> extra)
    {
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:LyriaDatabase"] =
                "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True",
            ["Serilog:WriteTo:1:Args:path"] =
                Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log")
        };

        foreach (KeyValuePair<string, string?> entry in extra)
        {
            settings[entry.Key] = entry.Value;
        }

        return settings;
    }

    [Fact]
    public async Task AllowedOrigin_ReceivesAllowOriginHeader()
    {
        HttpClient client = _factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Get, RestrictionsEndpoint);
        request.Headers.Add("Origin", AllowedOrigin);

        HttpResponseMessage response = await client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.True(response.Headers.TryGetValues(AllowOriginHeader, out IEnumerable<string>? values));
        Assert.Equal(AllowedOrigin, Assert.Single(values!));
    }

    [Fact]
    public async Task DisallowedOrigin_DoesNotReceiveAllowOriginHeader()
    {
        HttpClient client = _factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Get, RestrictionsEndpoint);
        request.Headers.Add("Origin", DisallowedOrigin);

        HttpResponseMessage response = await client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_IsAuthorized()
    {
        HttpClient client = _factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Options, RestrictionsEndpoint);
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        HttpResponseMessage response = await client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            AllowedOrigin,
            Assert.Single(response.Headers.GetValues(AllowOriginHeader)));

        string methods = string.Join(',', response.Headers.GetValues(AllowMethodsHeader));
        Assert.Contains("GET", methods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST", methods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUT", methods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PATCH", methods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE", methods, StringComparison.OrdinalIgnoreCase);

        string headers = string.Join(',', response.Headers.GetValues(AllowHeadersHeader));
        Assert.Contains("Accept", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content-Type", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Authorization", headers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_DoesNotReceiveAllowOriginHeader()
    {
        HttpClient client = _factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Options, RestrictionsEndpoint);
        request.Headers.Add("Origin", DisallowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        HttpResponseMessage response = await client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public async Task ExistingEndpoints_ContinueWorking_WithoutOriginHeader()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage restrictions = await client.GetAsync(
            RestrictionsEndpoint, TestContext.Current.CancellationToken);
        HttpResponseMessage health = await client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);
        HttpResponseMessage swagger = await client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.True(restrictions.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.False(restrictions.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public void Policy_IsRegisteredOnce_WithConfiguredOriginsAndNoWildcard()
    {
        IOptions<CorsOptions> options =
            _factory.Services.GetRequiredService<IOptions<CorsOptions>>();

        CorsPolicy? policy = options.Value.GetPolicy(CorsExtensions.PolicyName);

        Assert.NotNull(policy);
        // No debe existir una segunda política (por defecto) duplicada.
        Assert.Null(options.Value.GetPolicy(options.Value.DefaultPolicyName));
        Assert.False(policy!.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
        Assert.Equal([AllowedOrigin], policy.Origins);
        Assert.Equal(
            ["GET", "POST", "PUT", "PATCH", "DELETE"],
            policy.Methods);
        Assert.Equal(
            ["Accept", "Content-Type", "Authorization"],
            policy.Headers);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("http://localhost:5173/", 1)]
    public void NormalizeOrigins_HandlesEmptyAndTrailingSlashValues(
        string? origin,
        int expectedCount)
    {
        string[] result = CorsExtensions.NormalizeOrigins([origin]);

        Assert.Equal(expectedCount, result.Length);

        if (expectedCount == 1)
        {
            Assert.Equal(AllowedOrigin, result[0]);
        }
    }

    [Fact]
    public void NormalizeOrigins_WithNullCollection_ReturnsEmpty()
    {
        Assert.Empty(CorsExtensions.NormalizeOrigins(null));
    }

    [Fact]
    public void NormalizeOrigins_RemovesDuplicates()
    {
        string[] result = CorsExtensions.NormalizeOrigins(
            [AllowedOrigin, $"{AllowedOrigin}/", "HTTP://LOCALHOST:5173"]);

        Assert.Single(result);
    }
}

/// <summary>
/// Verifica el comportamiento seguro cuando "Cors:AllowedOrigins" está vacío:
/// la aplicación arranca y no autoriza ningún origen cruzado.
/// </summary>
[Collection(LyriaApiTestGroup.Name)]
public class CorsEmptyConfigurationTests
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private readonly WebApplicationFactory<Program> _factory;

    public CorsEmptyConfigurationTests(WebApplicationFactory<Program> factory)
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
                    ["Cors:AllowedOrigins:0"] = string.Empty
                });
            });
        });
    }

    [Fact]
    public async Task EmptyConfiguration_StartsAndBlocksEveryOrigin()
    {
        HttpClient client = _factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        HttpResponseMessage response = await client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    /// <summary>
    /// Reproduce el escenario del servidor publicado: entorno Production
    /// (sin appsettings.Development.json) y el origen definido por la variable
    /// de entorno Cors__AllowedOrigins__0.
    /// </summary>
    [Fact]
    public async Task EnvironmentVariable_InProduction_AuthorizesOrigin()
    {
        const string origin = "http://localhost:5173";
        const string variable = "Cors__AllowedOrigins__0";

        Environment.SetEnvironmentVariable(variable, origin);

        try
        {
            using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, config) => config.AddEnvironmentVariables());
            });

            HttpClient client = factory.CreateClient();

            using HttpRequestMessage request = new(HttpMethod.Get, "/api/health");
            request.Headers.Add("Origin", origin);

            HttpResponseMessage response = await client.SendAsync(
                request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                origin,
                Assert.Single(response.Headers.GetValues(AllowOriginHeader)));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void EmptyConfiguration_ProducesPolicyWithoutOrigins()
    {
        IOptions<CorsOptions> options =
            _factory.Services.GetRequiredService<IOptions<CorsOptions>>();

        CorsPolicy? policy = options.Value.GetPolicy(CorsExtensions.PolicyName);

        Assert.NotNull(policy);
        Assert.False(policy!.AllowAnyOrigin);
        Assert.Empty(policy.Origins);
    }
}
