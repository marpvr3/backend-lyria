using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Notifications;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Pruebas funcionales de la verificación de correo.
/// </summary>
/// <remarks>
/// Cada escenario levanta su propio host derivado del compartido. La clase los libera al
/// terminar cada prueba: sin ello, los hosts se acumularían durante toda la ejecución del
/// proyecto y acabarían agotando los recursos del proceso de pruebas.
/// </remarks>
[Collection(LyriaApiTestGroup.Name)]
public class EmailVerificationControllerTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
        GC.SuppressFinalize(this);
    }

    private const string ResendPath = "/api/v1/auth/email-verification/resend";
    private const string ConfirmPath = "/api/v1/auth/email-verification/confirm";
    private const string LoginPath = "/api/v1/auth/login";
    private const string MePath = "/api/v1/users/me";

    private const string Email = "andres@email.com";
    private const string Password = "Password123";

    private readonly WebApplicationFactory<Program> _factory;

    public EmailVerificationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // --- Escenario ---

    private Harness CreateHarness(
        UserStatus status = UserStatus.Unverified,
        bool seedUser = true,
        bool seedVerification = true,
        int resendPermitLimit = 1000,
        int confirmPermitLimit = 1000,
        int cooldownSeconds = 60)
    {
        var harness = new Harness();

        if (seedUser)
        {
            var user = User.Create(
                UserId.New(), "Andres", "Perez", Email,
                harness.PasswordHasher.Hash(Password), null, null, null);

            if (status != UserStatus.Unverified)
            {
                user.MarkEmailAsVerified();
                user.ChangeStatus(UserStatus.Active);
            }

            if (status is UserStatus.Suspended or UserStatus.Deleted)
            {
                user.ChangeStatus(UserStatus.Suspended);
            }

            if (status == UserStatus.Deleted)
            {
                user.ChangeStatus(UserStatus.Deleted);
            }

            harness.Users.Seed(user);
            harness.ReadService.Seed(user);
            harness.User = user;

            if (seedVerification && status == UserStatus.Unverified)
            {
                harness.SeedVerification(user.Id);
            }
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
                    ["EmailVerification:ResendCooldownSeconds"] =
                        cooldownSeconds.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:EmailVerificationResend:PermitLimit"] =
                        resendPermitLimit.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:EmailVerificationResend:WindowSeconds"] = "900",
                    ["RateLimiting:EmailVerificationConfirm:PermitLimit"] =
                        confirmPermitLimit.ToString(CultureInfo.InvariantCulture),
                    ["RateLimiting:EmailVerificationConfirm:WindowSeconds"] = "900"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IUserRepository>(harness.Users);
                services.AddSingleton<IUserReadService>(harness.ReadService);
                services.AddSingleton<IPasswordHasher>(harness.PasswordHasher);
                services.AddSingleton<IUserEmailVerificationRepository>(harness.Verifications);
                services.AddSingleton<IEmailVerificationWriter>(harness.Writer);
                services.AddSingleton<IEmailSender>(harness.EmailSender);
                services.AddSingleton<IUserRefreshTokenRepository>(harness.Sessions);
                services.AddSingleton<IAuthenticationSessionWriter>(harness.SessionWriter);
            });
        });

        harness.Client = configured.CreateClient();
        harness.Services = configured.Services;

        _disposables.Add(harness.Client);
        _disposables.Add(configured);

        return harness;
    }

    private static object ResendRequest(string? email = Email) => new { Email = email };

    private static object ConfirmRequest(string? email = Email, string? code = null) =>
        new { Email = email, Code = code };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

    // --- Reenvío ---

    [Fact]
    public async Task Resend_ForAPendingAccount_ReturnsAccepted()
    {
        Harness harness = CreateHarness(seedVerification: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);
        Assert.Equal(
            "Si existe una cuenta pendiente de verificación, se enviará un nuevo código.",
            body.GetProperty("message").GetString());

        Assert.Single(harness.EmailSender.SentEmails);
    }

    [Fact]
    public async Task Resend_ForAnUnknownEmail_ReturnsTheSameResponse()
    {
        Harness harness = CreateHarness(seedUser: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest("nadie@email.com"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(harness.EmailSender.SentEmails);
    }

    /// <summary>
    /// Las cinco situaciones deben producir respuestas indistinguibles byte a byte.
    /// Se comparan contra el mismo host para que la única diferencia sea la cuenta.
    /// </summary>
    [Fact]
    public async Task Resend_EveryOutcome_ProducesIdenticalResponses()
    {
        // La cuenta base (andres@email.com) queda pendiente de verificación.
        Harness harness = CreateHarness(seedVerification: false);

        harness.SeedUser("verificado@email.com", UserStatus.Active);
        harness.SeedUser("suspendido@email.com", UserStatus.Suspended);
        harness.SeedUser("eliminado@email.com", UserStatus.Deleted);

        List<(HttpStatusCode Status, string Body)> observed = [];

        foreach (string email in new[]
        {
            "nadie@email.com",
            "verificado@email.com",
            "suspendido@email.com",
            "eliminado@email.com",
            Email
        })
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                ResendPath, ResendRequest(email), TestContext.Current.CancellationToken);

            observed.Add((
                response.StatusCode,
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken)));
        }

        Assert.All(observed, entry => Assert.Equal(observed[0], entry));
        Assert.Equal(HttpStatusCode.Accepted, observed[0].Status);

        // Solo la cuenta pendiente recibió realmente un código.
        FakeEmailSender.SentEmail sent = Assert.Single(harness.EmailSender.SentEmails);
        Assert.Equal(Email, sent.RecipientEmail);
    }

    [Fact]
    public async Task Resend_ResponseNeverContainsTheCode()
    {
        Harness harness = CreateHarness(seedVerification: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        string code = harness.EmailSender.LastCodeFor(Email)!;

        Assert.DoesNotContain(code, body, StringComparison.Ordinal);
        Assert.DoesNotContain("codeHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("codeSecret", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resend_WithinTheCooldown_StillReturnsAcceptedWithoutSending()
    {
        Harness harness = CreateHarness(seedVerification: false, cooldownSeconds: 3600);

        HttpResponseMessage first = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        HttpResponseMessage second = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Single(harness.EmailSender.SentEmails);
    }

    [Fact]
    public async Task Resend_WhenTheEmailProviderFails_StillReturnsAccepted()
    {
        Harness harness = CreateHarness(seedVerification: false);
        harness.EmailSender.FailureToThrow =
            new InvalidOperationException("smtp://usuario:contraseña@servidor");

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Ningún detalle del proveedor llega al cliente.
        Assert.DoesNotContain("smtp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contraseña", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Resend_WithoutEmail_ReturnsBadRequest(string? email)
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(email), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Confirmación ---

    [Fact]
    public async Task Confirm_WithAValidCode_ReturnsNoContent()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(harness.User!.IsEmailVerified);
        Assert.Equal(UserStatus.Active, harness.User.Status);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_ReturnsBadRequestWithTheGenericError()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("EmailVerification.InvalidCode", problem.Extensions["code"]?.ToString());
        Assert.Equal("El código de verificación no es válido o ha vencido.", problem.Detail);

        Assert.Equal(UserStatus.Unverified, harness.User!.Status);
    }

    /// <summary>
    /// Los siete rechazos deben producir respuestas indistinguibles byte a byte.
    /// Se comparan contra el mismo host para que la única diferencia sea la cuenta.
    /// </summary>
    [Fact]
    public async Task Confirm_EveryRejection_ProducesIdenticalResponses()
    {
        // La cuenta base conserva un código vigente: se usará con un código incorrecto.
        Harness harness = CreateHarness();

        UserId expiredUserId = harness.SeedUser("vencido@email.com").Id;
        harness.SeedVerification(
            expiredUserId,
            createdAtUtc: DateTime.UtcNow.AddMinutes(-60),
            expiresAtUtc: DateTime.UtcNow.AddMinutes(-45));

        UserId usedUserId = harness.SeedUser("usado@email.com").Id;
        harness.SeedVerification(usedUserId).MarkAsUsed(DateTime.UtcNow.AddMinutes(-1));

        UserId revokedUserId = harness.SeedUser("revocado@email.com").Id;
        harness.SeedVerification(revokedUserId).Revoke(DateTime.UtcNow.AddMinutes(-1));

        UserId exhaustedUserId = harness.SeedUser("agotado@email.com").Id;
        UserEmailVerification exhausted = harness.SeedVerification(exhaustedUserId);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            exhausted.RegisterFailedAttempt();
        }

        harness.SeedUser("verificado@email.com", UserStatus.Active);
        harness.SeedUser("suspendido@email.com", UserStatus.Suspended);

        List<(HttpStatusCode Status, string Body)> observed = [];

        foreach ((string email, string code) in new[]
        {
            ("nadie@email.com", "482731"),
            (Email, "000000"),
            ("vencido@email.com", "482731"),
            ("usado@email.com", "482731"),
            ("revocado@email.com", "482731"),
            ("agotado@email.com", "482731"),
            ("verificado@email.com", "482731"),
            ("suspendido@email.com", "482731")
        })
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                ConfirmPath, ConfirmRequest(email, code),
                TestContext.Current.CancellationToken);

            observed.Add((
                response.StatusCode,
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken)));
        }

        Assert.All(observed, entry => Assert.Equal(observed[0], entry));
        Assert.Equal(HttpStatusCode.BadRequest, observed[0].Status);
    }

    /// <summary>
    /// El quinto intento fallido invalida el código: ni siquiera el correcto sirve luego.
    /// </summary>
    [Fact]
    public async Task Confirm_TheFifthFailedAttempt_InvalidatesTheCode()
    {
        Harness harness = CreateHarness();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage failed = await harness.Client.PostAsJsonAsync(
                ConfirmPath, ConfirmRequest(code: "000000"),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
        }

        HttpResponseMessage withCorrectCode = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, withCorrectCode.StatusCode);
        Assert.Equal(UserStatus.Unverified, harness.User!.Status);
    }

    [Fact]
    public async Task Confirm_TheCodeCannotBeUsedTwice()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage first = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        HttpResponseMessage second = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("")]
    public async Task Confirm_WithAMalformedCode_ReturnsBadRequest(string code)
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: code), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UserStatus.Unverified, harness.User!.Status);
    }

    [Fact]
    public async Task Confirm_AcceptsACodeWithLeadingZeros()
    {
        Harness harness = CreateHarness(seedVerification: false);
        harness.SeedVerification(harness.User!.Id, code: "000042");

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000042"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Ciclo completo con el inicio de sesión ---

    [Fact]
    public async Task Login_BeforeConfirming_ReturnsUnauthorized()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            LoginPath, new { Email, Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        JsonElement body = await ReadJsonAsync(response);
        Assert.Equal(
            "Authentication.InvalidCredentials", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_AfterConfirming_Succeeds()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage confirmed = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);

        HttpResponseMessage login = await harness.Client.PostAsJsonAsync(
            LoginPath, new { Email, Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        JsonElement body = await ReadJsonAsync(login);
        Assert.Equal("Active", body.GetProperty("user").GetProperty("status").GetString());
    }

    /// <summary>
    /// Tras confirmar e iniciar sesión, el access token emitido sirve para los endpoints
    /// protegidos.
    /// </summary>
    [Fact]
    public async Task Me_WorksAfterConfirmingAndLoggingIn()
    {
        Harness harness = CreateHarness();

        await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: harness.SeededCode),
            TestContext.Current.CancellationToken);

        HttpResponseMessage login = await harness.Client.PostAsJsonAsync(
            LoginPath, new { Email, Password }, TestContext.Current.CancellationToken);

        JsonElement tokens = await ReadJsonAsync(login);

        harness.Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", tokens.GetProperty("accessToken").GetString());

        HttpResponseMessage me = await harness.Client.GetAsync(
            MePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        JsonElement profile = await ReadJsonAsync(me);
        Assert.Equal("Active", profile.GetProperty("status").GetString());
        Assert.True(profile.GetProperty("isEmailVerified").GetBoolean());
    }

    // --- Limitación de solicitudes ---

    [Fact]
    public async Task Resend_BeyondTheLimit_ReturnsTooManyRequests()
    {
        Harness harness = CreateHarness(resendPermitLimit: 2);

        await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);
        await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        HttpResponseMessage rejected = await harness.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task Confirm_BeyondTheLimit_ReturnsTooManyRequests()
    {
        Harness harness = CreateHarness(confirmPermitLimit: 2);

        await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"), TestContext.Current.CancellationToken);
        await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"), TestContext.Current.CancellationToken);

        HttpResponseMessage rejected = await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    /// <summary>
    /// El rechazo por límite no revela si la cuenta existe.
    /// </summary>
    [Fact]
    public async Task RateLimitRejection_DoesNotRevealWhetherTheAccountExists()
    {
        Harness known = CreateHarness(resendPermitLimit: 1);
        Harness unknown = CreateHarness(seedUser: false, resendPermitLimit: 1);

        await known.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);
        await unknown.Client.PostAsJsonAsync(
            ResendPath, ResendRequest("nadie@email.com"),
            TestContext.Current.CancellationToken);

        HttpResponseMessage knownRejected = await known.Client.PostAsJsonAsync(
            ResendPath, ResendRequest(), TestContext.Current.CancellationToken);
        HttpResponseMessage unknownRejected = await unknown.Client.PostAsJsonAsync(
            ResendPath, ResendRequest("nadie@email.com"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, knownRejected.StatusCode);
        Assert.Equal(
            await knownRejected.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken),
            await unknownRejected.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// La limitación de la verificación no afecta al inicio de sesión: son políticas
    /// independientes.
    /// </summary>
    [Fact]
    public async Task TheVerificationLimit_DoesNotAffectLogin()
    {
        Harness harness = CreateHarness(UserStatus.Active, confirmPermitLimit: 1);

        await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"), TestContext.Current.CancellationToken);
        await harness.Client.PostAsJsonAsync(
            ConfirmPath, ConfirmRequest(code: "000000"), TestContext.Current.CancellationToken);

        HttpResponseMessage login = await harness.Client.PostAsJsonAsync(
            LoginPath, new { Email, Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    // --- Los endpoints son anónimos ---

    [Theory]
    [InlineData(ResendPath)]
    [InlineData(ConfirmPath)]
    public async Task TheEndpoints_DoNotRequireAuthentication(string path)
    {
        Harness harness = CreateHarness();

        object request = path == ResendPath
            ? ResendRequest()
            : ConfirmRequest(code: "000000");

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            path, request, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Fakes ---

    private sealed class Harness
    {
        private readonly List<UserEmailVerification> _verifications = [];

        public FakeUserRepository Users { get; } = new();
        public FakeUserReadService ReadService { get; } = new();
        public FakePasswordHasher PasswordHasher { get; } = new();
        public FakeVerificationRepository Verifications { get; }
        public FakeVerificationWriter Writer { get; }
        public FakeEmailSender EmailSender { get; } = new();
        public FakeSessionRepository Sessions { get; } = new();
        public FakeSessionWriter SessionWriter { get; }

        public HttpClient Client { get; set; } = null!;
        public IServiceProvider Services { get; set; } = null!;
        public User? User { get; set; }

        /// <summary>
        /// Código en claro de la verificación sembrada. Solo existe en la prueba: la API
        /// nunca lo devuelve.
        /// </summary>
        public string SeededCode { get; private set; } = "482731";

        public Harness()
        {
            Verifications = new FakeVerificationRepository(_verifications);
            Writer = new FakeVerificationWriter(_verifications);
            SessionWriter = new FakeSessionWriter(Sessions);
        }

        /// <summary>
        /// Siembra una cuenta adicional en el mismo escenario, para comparar respuestas
        /// sin levantar un host por cada situación.
        /// </summary>
        public User SeedUser(string email, UserStatus status = UserStatus.Unverified)
        {
            var user = User.Create(
                UserId.New(), "Otro", "Usuario", email,
                PasswordHasher.Hash(Password), null, null, null);

            if (status != UserStatus.Unverified)
            {
                user.MarkEmailAsVerified();
                user.ChangeStatus(UserStatus.Active);
            }

            if (status is UserStatus.Suspended or UserStatus.Deleted)
            {
                user.ChangeStatus(UserStatus.Suspended);
            }

            if (status == UserStatus.Deleted)
            {
                user.ChangeStatus(UserStatus.Deleted);
            }

            Users.Seed(user);
            ReadService.Seed(user);

            return user;
        }

        public UserEmailVerification SeedVerification(
            UserId userId,
            string code = "482731",
            DateTime? createdAtUtc = null,
            DateTime? expiresAtUtc = null)
        {
            SeededCode = code;

            DateTime created = createdAtUtc ?? DateTime.UtcNow.AddMinutes(-1);

            var verification = UserEmailVerification.Create(
                UserEmailVerificationId.New(),
                userId,
                HashOf(code),
                created,
                expiresAtUtc ?? created.AddMinutes(15));

            _verifications.Add(verification);

            return verification;
        }

        /// <summary>
        /// Reproduce el hash real usando el mismo secreto que configuró el host de pruebas.
        /// </summary>
        private static string HashOf(string code)
        {
            string secret =
                Environment.GetEnvironmentVariable("EmailVerification__CodeSecret")!;

            return Convert.ToHexStringLower(
                System.Security.Cryptography.HMACSHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(secret),
                    System.Text.Encoding.UTF8.GetBytes(code)));
        }
    }

    private sealed class FakeVerificationRepository(List<UserEmailVerification> verifications)
        : IUserEmailVerificationRepository
    {
        public Task<UserEmailVerification?> GetLatestPendingAsync(
            UserId userId, CancellationToken cancellationToken) =>
            Task.FromResult(Pending(userId).FirstOrDefault());

        public Task<IReadOnlyList<UserEmailVerification>> GetPendingAsync(
            UserId userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserEmailVerification>>([.. Pending(userId)]);

        public Task<DateTime?> GetLastCreatedAtUtcAsync(
            UserId userId, CancellationToken cancellationToken) =>
            Task.FromResult(verifications
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAtUtc)
                .Select(v => (DateTime?)v.CreatedAtUtc)
                .FirstOrDefault());

        private IEnumerable<UserEmailVerification> Pending(UserId userId) =>
            verifications
                .Where(v => v.UserId == userId && !v.IsUsed && !v.IsRevoked)
                .OrderByDescending(v => v.CreatedAtUtc);
    }

    /// <summary>
    /// Escritor en memoria. Las entidades ya vienen mutadas por el caso de uso; solo
    /// hace falta incorporar la verificación nueva al almacén compartido.
    /// </summary>
    private sealed class FakeVerificationWriter(List<UserEmailVerification> verifications)
        : IEmailVerificationWriter
    {
        public Task ResendAsync(
            IReadOnlyCollection<UserEmailVerification> revokedVerifications,
            UserEmailVerification createdVerification,
            CancellationToken cancellationToken)
        {
            verifications.Add(createdVerification);
            return Task.CompletedTask;
        }

        public Task<bool> TryConfirmAsync(
            User user,
            UserEmailVerification verification,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task RegisterFailedAttemptAsync(
            UserEmailVerification verification,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSessionRepository : IUserRefreshTokenRepository
    {
        private readonly List<Domain.Users.RefreshTokens.UserRefreshToken> _sessions = [];

        public void Add(Domain.Users.RefreshTokens.UserRefreshToken session) =>
            _sessions.Add(session);

        public Task<Domain.Users.RefreshTokens.UserRefreshToken?> GetByTokenHashAsync(
            string tokenHash, CancellationToken cancellationToken)
        {
            string normalized =
                Domain.Users.RefreshTokens.UserRefreshToken.NormalizeTokenHash(tokenHash);

            return Task.FromResult(_sessions.FirstOrDefault(
                s => string.Equals(s.TokenHash, normalized, StringComparison.Ordinal)));
        }
    }

    private sealed class FakeSessionWriter(FakeSessionRepository sessions)
        : IAuthenticationSessionWriter
    {
        public Task CompleteLoginAsync(
            User user,
            Domain.Users.RefreshTokens.UserRefreshToken session,
            CancellationToken cancellationToken)
        {
            sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task RotateAsync(
            Domain.Users.RefreshTokens.UserRefreshToken revokedSession,
            Domain.Users.RefreshTokens.UserRefreshToken createdSession,
            CancellationToken cancellationToken)
        {
            sessions.Add(createdSession);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(
            Domain.Users.RefreshTokens.UserRefreshToken revokedSession,
            CancellationToken cancellationToken) => Task.CompletedTask;
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

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(
            string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByEmailAsync(
            string normalizedEmail, UserId? excludingId, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Any(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
