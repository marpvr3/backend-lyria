using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class AuthenticationControllerTests
{
    private const string LoginPath = "/api/v1/auth/login";
    private const string RefreshPath = "/api/v1/auth/refresh";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string MePath = "/api/v1/users/me";

    private const string Email = "andres@email.com";
    private const string Password = "Password123";

    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // --- Escenario ---

    /// <summary>
    /// Prepara el escenario. El estado predeterminado es <see cref="UserStatus.Active"/>:
    /// desde que existe la verificación de correo, es el único que permite autenticarse.
    /// </summary>
    private Harness CreateHarness(
        UserStatus status = UserStatus.Active,
        bool seedUser = true,
        int permitLimit = 1000)
    {
        var harness = new Harness();

        if (seedUser)
        {
            var user = User.Create(
                UserId.New(), "Andres", "Perez", Email,
                harness.PasswordHasher.Hash(Password), "3001234567",
                new DateOnly(1978, 12, 25), null);

            MoveToStatus(user, status);

            harness.Users.Seed(user);
            harness.ReadService.Seed(user);
            harness.User = user;
        }

        WebApplicationFactory<Program> configured = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True",
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log"),
                    ["RateLimiting:Authentication:PermitLimit"] =
                        permitLimit.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:Authentication:WindowSeconds"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IUserRepository>(harness.Users);
                services.AddSingleton<IUserReadService>(harness.ReadService);
                services.AddSingleton<IPasswordHasher>(harness.PasswordHasher);
                services.AddSingleton<IUserRefreshTokenRepository>(harness.Sessions);
                services.AddSingleton<IAuthenticationSessionWriter>(harness.Writer);
            });
        });

        harness.Client = configured.CreateClient();
        harness.Services = configured.Services;

        return harness;
    }

    private static void MoveToStatus(User user, UserStatus status)
    {
        if (status == UserStatus.Unverified)
        {
            return;
        }

        // Activar una cuenta solo es posible confirmando el correo, de modo que el
        // escenario reproduce ambos efectos juntos.
        user.MarkEmailAsVerified();
        user.ChangeStatus(UserStatus.Active);

        if (status == UserStatus.Active)
        {
            return;
        }

        user.ChangeStatus(UserStatus.Suspended);

        if (status == UserStatus.Suspended)
        {
            return;
        }

        user.ChangeStatus(UserStatus.Deleted);
    }

    private static object Credentials(string? email = Email, string? password = Password) =>
        new { Email = email, Password = password };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

    private static async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        Harness harness)
    {
        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);

        return (
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!);
    }

    // --- Login ---

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndBothTokens()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);

        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
        Assert.True(body.TryGetProperty("accessTokenExpiresAtUtc", out _));
        Assert.True(body.TryGetProperty("refreshTokenExpiresAtUtc", out _));

        JsonElement user = body.GetProperty("user");
        Assert.Equal(harness.User!.Id.Value, user.GetProperty("userId").GetGuid());
        Assert.Equal("Andres", user.GetProperty("name").GetString());
        Assert.Equal("Perez", user.GetProperty("lastName").GetString());
        Assert.Equal(Email, user.GetProperty("email").GetString());
        Assert.Equal("Active", user.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Login_ResponseNeverExposesSecrets()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(Password, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tokenHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signingKey", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "Incorrecta999"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);
        Assert.Equal("Authentication.InvalidCredentials", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// Correo inexistente y contraseña incorrecta deben producir respuestas
    /// indistinguibles byte a byte.
    /// </summary>
    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ProduceIdenticalResponses()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage unknownEmail = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(email: "nadie@email.com"),
            TestContext.Current.CancellationToken);

        HttpResponseMessage wrongPassword = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "Incorrecta999"),
            TestContext.Current.CancellationToken);

        Assert.Equal(unknownEmail.StatusCode, wrongPassword.StatusCode);
        Assert.Equal(
            await unknownEmail.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            await wrongPassword.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(UserStatus.Unverified)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deleted)]
    public async Task Login_WithNonAuthenticableStatus_ReturnsUnauthorized(UserStatus status)
    {
        Harness harness = CreateHarness(status);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Cuenta sin verificar, suspendida y eliminada deben producir respuestas
    /// indistinguibles byte a byte: ninguna revela por qué se rechazó el acceso.
    /// </summary>
    [Fact]
    public async Task Login_UnverifiedSuspendedAndDeleted_ProduceIdenticalResponses()
    {
        Harness unverified = CreateHarness(UserStatus.Unverified);
        Harness suspended = CreateHarness(UserStatus.Suspended);
        Harness deleted = CreateHarness(UserStatus.Deleted);

        HttpResponseMessage unverifiedResponse = await unverified.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        HttpResponseMessage suspendedResponse = await suspended.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        HttpResponseMessage deletedResponse = await deleted.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        string unverifiedBody = await unverifiedResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        string suspendedBody = await suspendedResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        string deletedBody = await deletedResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(unverifiedResponse.StatusCode, suspendedResponse.StatusCode);
        Assert.Equal(unverifiedResponse.StatusCode, deletedResponse.StatusCode);
        Assert.Equal(unverifiedBody, suspendedBody);
        Assert.Equal(unverifiedBody, deletedBody);
    }

    /// <summary>
    /// El rechazo de una cuenta sin verificar tampoco puede distinguirse del de una
    /// contraseña incorrecta.
    /// </summary>
    [Fact]
    public async Task Login_UnverifiedAndWrongPassword_ProduceIdenticalResponses()
    {
        Harness unverified = CreateHarness(UserStatus.Unverified);
        Harness active = CreateHarness();

        HttpResponseMessage unverifiedResponse = await unverified.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        HttpResponseMessage wrongPassword = await active.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "Incorrecta999"),
            TestContext.Current.CancellationToken);

        Assert.Equal(unverifiedResponse.StatusCode, wrongPassword.StatusCode);
        Assert.Equal(
            await unverifiedResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken),
            await wrongPassword.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Un rechazo por cuenta sin verificar no abre sesión de ninguna clase.
    /// </summary>
    [Fact]
    public async Task Login_WithUnverifiedStatus_DoesNotCreateAnySession()
    {
        Harness harness = CreateHarness(UserStatus.Unverified);

        await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        Assert.Empty(harness.Sessions.Sessions);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData(Email, "")]
    public async Task Login_WithMissingFields_ReturnsBadRequest(string email, string password)
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(email, password), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Refresh ---

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOkAndRotatesTheToken()
    {
        Harness harness = CreateHarness();
        (_, string refreshToken) = await LoginAsync(harness);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            RefreshPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);

        Assert.NotEqual(refreshToken, body.GetProperty("refreshToken").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            RefreshPath, new { RefreshToken = "token-inexistente" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);
        Assert.Equal("Authentication.InvalidRefreshToken", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Refresh_ReusingARotatedToken_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();
        (_, string refreshToken) = await LoginAsync(harness);

        HttpResponseMessage first = await harness.Client.PostAsJsonAsync(
            RefreshPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        HttpResponseMessage second = await harness.Client.PostAsJsonAsync(
            RefreshPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    // --- Logout ---

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        Harness harness = CreateHarness();
        (_, string refreshToken) = await LoginAsync(harness);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LogoutPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RepeatedOrUnknown_AlwaysReturnsNoContent()
    {
        Harness harness = CreateHarness();
        (_, string refreshToken) = await LoginAsync(harness);

        HttpResponseMessage first = await harness.Client.PostAsJsonAsync(
            LogoutPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        HttpResponseMessage second = await harness.Client.PostAsJsonAsync(
            LogoutPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        HttpResponseMessage unknown = await harness.Client.PostAsJsonAsync(
            LogoutPath, new { RefreshToken = "token-inexistente" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheSessionSoRefreshStopsWorking()
    {
        Harness harness = CreateHarness();
        (_, string refreshToken) = await LoginAsync(harness);

        await harness.Client.PostAsJsonAsync(
            LogoutPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        HttpResponseMessage refresh = await harness.Client.PostAsJsonAsync(
            RefreshPath, new { RefreshToken = refreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    // --- /users/me ---

    [Fact]
    public async Task Me_WithValidToken_ReturnsTheProfile()
    {
        Harness harness = CreateHarness();
        (string accessToken, _) = await LoginAsync(harness);

        harness.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await harness.Client.GetAsync(
            MePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);

        Assert.Equal(harness.User!.Id.Value, body.GetProperty("userId").GetGuid());
        Assert.Equal("Andres", body.GetProperty("name").GetString());
        Assert.Equal("Perez", body.GetProperty("lastName").GetString());
        Assert.Equal(Email, body.GetProperty("email").GetString());
        Assert.Equal("3001234567", body.GetProperty("phone").GetString());
        Assert.Equal("1978-12-25", body.GetProperty("birthDate").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("photoUrl").ValueKind);
        Assert.Equal("Active", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("isEmailVerified").GetBoolean());
    }

    [Fact]
    public async Task Me_ResponseDoesNotIncludeOutOfScopeData()
    {
        Harness harness = CreateHarness();
        (string accessToken, _) = await LoginAsync(harness);

        harness.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await harness.Client.GetAsync(
            MePath, TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        foreach (string forbidden in new[]
        {
            "passwordHash", "hashContrasena", "restriction", "favorite",
            "review", "role", "session", "refreshToken"
        })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.GetAsync(
            MePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithMalformedToken_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        harness.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "no-es-un-jwt");

        HttpResponseMessage response = await harness.Client.GetAsync(
            MePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithExpiredToken_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithInvalidIssuer_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(issuer: "Otro.Emisor");

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithInvalidAudience_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(audience: "Otra.Audiencia");

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithInvalidSignature_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(
            signingKey: Convert.ToBase64String(new byte[64]));

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    /// <summary>
    /// El token es criptográficamente válido pero no acredita ninguna identidad: la
    /// autenticación pasa y es el caso de uso quien lo rechaza.
    /// </summary>
    [Fact]
    public async Task Me_WithTokenWithoutSubject_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(includeSubject: false);

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithNonGuidSubject_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(subject: "no-es-un-guid");

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithEmptyGuidSubject_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(subject: Guid.Empty.ToString());

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Me_WithTokenForAnUnknownUser_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        string token = harness.CreateToken(subject: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Unauthorized, await harness.GetMeStatusAsync(token));
    }

    // --- Rate limiting ---

    [Fact]
    public async Task Login_WhenTheLimitIsExceeded_ReturnsTooManyRequests()
    {
        Harness harness = CreateHarness(permitLimit: 2);

        await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "mala1"), TestContext.Current.CancellationToken);
        await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "mala2"), TestContext.Current.CancellationToken);

        HttpResponseMessage limited = await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(password: "mala3"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task Login_WithinTheLimit_IsNotAffected()
    {
        Harness harness = CreateHarness(permitLimit: 5);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                LoginPath, Credentials(), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RateLimiting_DoesNotAffectUnrelatedEndpoints()
    {
        Harness harness = CreateHarness(permitLimit: 1);

        // Se agota el límite de autenticación.
        await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);
        await harness.Client.PostAsJsonAsync(
            LoginPath, Credentials(), TestContext.Current.CancellationToken);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage health = await harness.Client.GetAsync(
                "/api/health", TestContext.Current.CancellationToken);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, health.StatusCode);
        }
    }

    [Fact]
    public async Task Logout_IsNotRateLimited()
    {
        Harness harness = CreateHarness(permitLimit: 1);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                LogoutPath, new { RefreshToken = "token-inexistente" },
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // --- Endpoints públicos intactos ---

    [Fact]
    public async Task AnonymousEndpoints_KeepWorkingWithoutAToken()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage health = await harness.Client.GetAsync(
            "/api/health", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, health.StatusCode);

        // El registro móvil sigue siendo anónimo: falla por validación, no por 401.
        HttpResponseMessage registration = await harness.Client.PostAsJsonAsync(
            "/api/v1/mobile/registrations", new { },
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, registration.StatusCode);
    }

    [Fact]
    public async Task AuthenticationEndpoints_AreAnonymous()
    {
        Harness harness = CreateHarness();

        foreach (string path in new[] { LoginPath, RefreshPath, LogoutPath })
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                path, new { }, TestContext.Current.CancellationToken);

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    // --- Harness ---

    private sealed class Harness
    {
        public FakeUserRepository Users { get; } = new();
        public FakeUserReadService ReadService { get; } = new();
        public FakePasswordHasher PasswordHasher { get; } = new();
        public FakeUserRefreshTokenRepository Sessions { get; } = new();
        public FakeAuthenticationSessionWriter Writer { get; }

        public HttpClient Client { get; set; } = null!;
        public IServiceProvider Services { get; set; } = null!;
        public User? User { get; set; }

        public Harness()
        {
            Writer = new FakeAuthenticationSessionWriter(Sessions);
        }

        public async Task<HttpStatusCode> GetMeStatusAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await Client.SendAsync(
                request, TestContext.Current.CancellationToken);

            return response.StatusCode;
        }

        /// <summary>
        /// Fabrica un access token con parámetros arbitrarios para probar cada
        /// validación del middleware por separado.
        /// </summary>
        public string CreateToken(
            string? subject = null,
            string? issuer = null,
            string? audience = null,
            string? signingKey = null,
            DateTime? expiresAtUtc = null,
            bool includeSubject = true)
        {
            IConfiguration configuration = Services.GetRequiredService<IConfiguration>();

            var claims = new Dictionary<string, object>(StringComparer.Ordinal);

            if (includeSubject)
            {
                claims[JwtRegisteredClaimNames.Sub] = subject
                    ?? User?.Id.Value.ToString()
                    ?? Guid.NewGuid().ToString();
            }

            claims[JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString();

            DateTime now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer ?? configuration["Jwt:Issuer"],
                Audience = audience ?? configuration["Jwt:Audience"],
                IssuedAt = now.AddMinutes(-2),
                NotBefore = now.AddMinutes(-2),
                Expires = expiresAtUtc ?? now.AddMinutes(15),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        signingKey ?? configuration["Jwt:SigningKey"]!)),
                    SecurityAlgorithms.HmacSha256),
                Claims = claims
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }

    // --- Fakes ---

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(
            string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByEmailAsync(
            string normalizedEmail, UserId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserReadService : IUserReadService
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<Application.Features.Users.UserResponse?> GetByIdAsync(
            UserId id, CancellationToken cancellationToken)
        {
            User? user = _users.FirstOrDefault(u => u.Id == id);

            return Task.FromResult(user is null
                ? null
                : new Application.Features.Users.UserResponse(
                    user.Id.Value, user.Name, user.LastName, user.Email, user.Phone,
                    user.BirthDate, user.PhotoUrl, user.Status.ToString(),
                    user.IsEmailVerified, user.LastLoginAtUtc, user.CreatedAtUtc,
                    user.UpdatedAtUtc));
        }

        public Task<Application.Common.PagedResponse<
            Application.Features.Users.UserListItemResponse>> ListAsync(
            Application.Features.Users.UserListFilter filter,
            CancellationToken cancellationToken)
            => Task.FromResult(
                new Application.Common.PagedResponse<
                    Application.Features.Users.UserListItemResponse>(
                    [], filter.Page, filter.PageSize, 0));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string NonMatchingHash => "hash-que-nunca-coincide";

        public string Hash(string password) => "hashed_" + password;

        public PasswordVerificationOutcome Verify(
            string hashedPassword, string providedPassword) =>
            string.Equals(hashedPassword, Hash(providedPassword), StringComparison.Ordinal)
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed;
    }

    private sealed class FakeUserRefreshTokenRepository : IUserRefreshTokenRepository
    {
        private readonly List<UserRefreshToken> _sessions = [];

        public IReadOnlyList<UserRefreshToken> Sessions => _sessions;

        public void Add(UserRefreshToken session) => _sessions.Add(session);

        public Task<UserRefreshToken?> GetByTokenHashAsync(
            string tokenHash, CancellationToken cancellationToken)
        {
            string normalized = UserRefreshToken.NormalizeTokenHash(tokenHash);

            return Task.FromResult(_sessions.FirstOrDefault(
                s => string.Equals(s.TokenHash, normalized, StringComparison.Ordinal)));
        }
    }

    /// <summary>
    /// Escritor en memoria que sí conserva las sesiones, para que el ciclo completo
    /// login → refresh → logout funcione de extremo a extremo sin base de datos.
    /// </summary>
    private sealed class FakeAuthenticationSessionWriter(
        FakeUserRefreshTokenRepository sessions) : IAuthenticationSessionWriter
    {
        public Task CompleteLoginAsync(
            User user, UserRefreshToken session, CancellationToken cancellationToken)
        {
            sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task RotateAsync(
            UserRefreshToken revokedSession,
            UserRefreshToken createdSession,
            CancellationToken cancellationToken)
        {
            sessions.Add(createdSession);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(
            UserRefreshToken revokedSession, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
