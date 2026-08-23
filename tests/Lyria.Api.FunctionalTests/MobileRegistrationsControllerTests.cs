using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Notifications;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class MobileRegistrationsControllerTests
{
    private const string BasePath = "/api/v1/mobile/registrations";
    private const string AdminUsersPath = "/api/v1/users";
    private const string PlainPassword = "Password123";

    private static readonly Guid ConfiguredRoleId =
        Guid.Parse("b7e61e8b-7a94-4638-9068-cf364b81ee31");

    private static readonly Guid RestrictionId1 =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly Guid RestrictionId2 =
        Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static readonly Guid InactiveRestrictionId =
        Guid.Parse("00000000-0000-0000-0000-000000000009");

    private readonly WebApplicationFactory<Program> _factory;

    public MobileRegistrationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // --- Arranque del escenario ---

    private Harness CreateHarness(
        string? configuredRoleId = null,
        string importanceLevel = UserRestrictionImportanceLevels.High,
        bool seedRole = true,
        bool roleIsActive = true,
        string? existingEmail = null)
    {
        var harness = new Harness();

        if (seedRole)
        {
            var role = Role.Create(new RoleId(ConfiguredRoleId), "Usuario", null);

            if (!roleIsActive)
            {
                role.Deactivate();
            }

            harness.Roles.Seed(role);
        }

        harness.Restrictions.Seed(
            Restriction.Create(new RestrictionId(RestrictionId1), "Sin lactosa", null));
        harness.Restrictions.Seed(
            Restriction.Create(new RestrictionId(RestrictionId2), "Sin gluten", null));

        var inactive = Restriction.Create(
            new RestrictionId(InactiveRestrictionId), "Retirada", null);
        inactive.Deactivate();
        harness.Restrictions.Seed(inactive);

        if (existingEmail is not null)
        {
            harness.Users.Seed(User.Create(
                UserId.New(), "Otro", "Usuario", existingEmail, "hash", null, null, null));
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
                    ["MobileRegistration:DefaultRoleId"] =
                        configuredRoleId ?? ConfiguredRoleId.ToString(),
                    ["MobileRegistration:DefaultRestrictionImportanceLevel"] = importanceLevel
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IUserRepository>(harness.Users);
                services.AddSingleton<IRoleRepository>(harness.Roles);
                services.AddSingleton<IRestrictionRepository>(harness.Restrictions);
                services.AddSingleton<IMobileRegistrationWriter>(harness.Writer);
                services.AddSingleton<IPasswordHasher>(harness.PasswordHasher);
                services.AddSingleton<IEmailSender>(harness.EmailSender);
            });
        });

        harness.Client = configured.CreateClient();
        return harness;
    }

    private static object ValidRequest(object? restrictionIds = null) => new
    {
        Name = "Andres",
        LastName = "Perez",
        Email = $"andres-{Guid.NewGuid():N}@email.com",
        Password = PlainPassword,
        Phone = "3001234567",
        BirthDate = "1978-12-25",
        PhotoUrl = (string?)null,
        RestrictionIds = restrictionIds ?? Array.Empty<Guid>()
    };

    // --- 201 Created ---

    [Fact]
    public async Task Register_WithoutRestrictions_ReturnsCreated()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(harness.Writer.Committed);
        Assert.Empty(harness.Writer.CommittedRestrictions);
    }

    [Fact]
    public async Task Register_WithOneRestriction_ReturnsCreated()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(harness.Writer.CommittedRestrictions);
    }

    [Fact]
    public async Task Register_WithSeveralRestrictions_AssociatesAllOfThem()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1, RestrictionId2 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            [new RestrictionId(RestrictionId1), new RestrictionId(RestrictionId2)],
            harness.Writer.CommittedRestrictions.Select(ur => ur.RestrictionId));
    }

    [Fact]
    public async Task Register_ReturnsLocationHeaderAndPublicPayload()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, body.GetProperty("userId").GetGuid());
        Assert.Equal("Andres", body.GetProperty("name").GetString());
        Assert.Equal("Perez", body.GetProperty("lastName").GetString());
        Assert.Equal("Unverified", body.GetProperty("status").GetString());
        Assert.Single(body.GetProperty("restrictionIds").EnumerateArray());
    }

    [Fact]
    public async Task Register_ResponseNeverContainsPasswordOrHash()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(PlainPassword, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roleId", body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Rol asignado por el backend ---

    [Fact]
    public async Task Register_AssignsExactlyTheConfiguredRole_WithGlobalScope()
    {
        Harness harness = CreateHarness();

        await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        UserRole userRole = Assert.IsType<UserRole>(harness.Writer.CommittedUserRole);
        Assert.Equal(new RoleId(ConfiguredRoleId), userRole.RoleId);
        Assert.Equal(ScopeType.Global, userRole.ScopeType);
        Assert.Null(userRole.EstablishmentId);
        Assert.Null(userRole.BranchId);
        Assert.True(userRole.IsActive);
    }

    [Fact]
    public async Task Register_IgnoresRoleIdSentAsExtraField()
    {
        Harness harness = CreateHarness();
        var otherRoleId = Guid.NewGuid();

        var request = new
        {
            Name = "Andres",
            LastName = "Perez",
            Email = $"andres-{Guid.NewGuid():N}@email.com",
            Password = PlainPassword,
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null,
            RestrictionIds = Array.Empty<Guid>(),
            RoleId = otherRoleId,
            RoleName = "Administrador",
            IsAdmin = true
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            new RoleId(ConfiguredRoleId),
            Assert.IsType<UserRole>(harness.Writer.CommittedUserRole).RoleId);
    }

    [Fact]
    public async Task Register_IgnoresImportanceLevelSentAsExtraField()
    {
        Harness harness = CreateHarness();

        var request = new
        {
            Name = "Andres",
            LastName = "Perez",
            Email = $"andres-{Guid.NewGuid():N}@email.com",
            Password = PlainPassword,
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null,
            RestrictionIds = new[] { RestrictionId1, RestrictionId2 },
            ImportanceLevel = "Low",
            Status = "Active",
            IsEmailVerified = true,
            PasswordHash = "hash-inyectado"
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.All(
            harness.Writer.CommittedRestrictions,
            ur => Assert.Equal(UserRestrictionImportanceLevels.High, ur.ImportanceLevel));

        User user = Assert.IsType<User>(harness.Writer.CommittedUser);
        Assert.Equal(UserStatus.Unverified, user.Status);
        Assert.False(user.IsEmailVerified);
        Assert.Equal("hashed_" + PlainPassword, user.PasswordHash);
    }

    [Fact]
    public async Task Register_AppliesConfiguredImportanceLevel()
    {
        Harness harness = CreateHarness(
            importanceLevel: UserRestrictionImportanceLevels.Medium);

        await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            UserRestrictionImportanceLevels.Medium,
            Assert.Single(harness.Writer.CommittedRestrictions).ImportanceLevel);
    }

    // --- 409 Conflict ---

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        const string email = "duplicado@email.com";
        Harness harness = CreateHarness(existingEmail: email);

        var request = new
        {
            Name = "Andres",
            LastName = "Perez",
            Email = email,
            Password = PlainPassword,
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null,
            RestrictionIds = Array.Empty<Guid>()
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(harness.Writer.Committed);

        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "MobileRegistrations.EmailAlreadyExists",
            problem.Extensions["code"]?.ToString());
    }

    // --- 400 Bad Request ---

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    public async Task Register_WithInvalidPassword_ReturnsBadRequest(string password)
    {
        Harness harness = CreateHarness();

        var request = new
        {
            Name = "Andres",
            LastName = "Perez",
            Email = $"andres-{Guid.NewGuid():N}@email.com",
            Password = password,
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null,
            RestrictionIds = Array.Empty<Guid>()
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    [Fact]
    public async Task Register_WithEmptyRestrictionId_ReturnsBadRequest()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { Guid.Empty }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    [Fact]
    public async Task Register_WithDuplicatedRestrictionIds_ReturnsBadRequest()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1, RestrictionId1 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    [Fact]
    public async Task Register_WithEmptyName_ReturnsBadRequest()
    {
        Harness harness = CreateHarness();

        var request = new
        {
            Name = "",
            LastName = "Perez",
            Email = $"andres-{Guid.NewGuid():N}@email.com",
            Password = PlainPassword,
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null,
            RestrictionIds = Array.Empty<Guid>()
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- 404 Not Found ---

    [Fact]
    public async Task Register_WhenRestrictionDoesNotExist_ReturnsNotFound()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1, Guid.NewGuid() }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    [Fact]
    public async Task Register_WhenRestrictionIsInactive_ReturnsNotFound()
    {
        Harness harness = CreateHarness();

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { InactiveRestrictionId }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    // --- Errores de configuración del rol: todos idénticos y controlados ---

    [Fact]
    public async Task Register_WhenConfiguredRoleDoesNotExist_ReturnsControlledError()
    {
        Harness harness = CreateHarness(seedRole: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(harness.Writer.Committed);

        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "MobileRegistrations.RoleConfigurationError",
            problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Register_WhenConfiguredRoleIsInactive_ReturnsControlledError()
    {
        Harness harness = CreateHarness(roleIsActive: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(harness.Writer.Committed);

        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "MobileRegistrations.RoleConfigurationError",
            problem.Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Register_WhenDefaultRoleIdIsNotConfigured_ReturnsControlledError(
        string configuredRoleId)
    {
        Harness harness = CreateHarness(configuredRoleId: configuredRoleId);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(harness.Writer.Committed);

        ProblemDetails? problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "MobileRegistrations.RoleConfigurationError",
            problem.Extensions["code"]?.ToString());
    }

    /// <summary>
    /// Los cuatro problemas del rol configurado deben producir respuestas HTTP
    /// indistinguibles: mismo código de estado, mismo código de error y mismo detalle.
    /// </summary>
    [Fact]
    public async Task Register_AllRoleConfigurationProblems_ReturnIdenticalResponses()
    {
        List<(HttpStatusCode Status, string? Code, string? Detail)> observed = [];

        foreach (Harness harness in new[]
        {
            CreateHarness(configuredRoleId: ""),
            CreateHarness(configuredRoleId: "no-es-un-guid"),
            CreateHarness(seedRole: false),
            CreateHarness(roleIsActive: false)
        })
        {
            HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
                BasePath, ValidRequest(), TestContext.Current.CancellationToken);

            ProblemDetails? problem = await response.Content
                .ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

            observed.Add((
                response.StatusCode,
                problem?.Extensions["code"]?.ToString(),
                problem?.Detail));

            Assert.False(harness.Writer.Committed);
        }

        Assert.Equal(4, observed.Count);
        Assert.All(observed, entry => Assert.Equal(observed[0], entry));
        Assert.Equal(HttpStatusCode.InternalServerError, observed[0].Status);
        Assert.Equal("MobileRegistrations.RoleConfigurationError", observed[0].Code);
    }

    [Fact]
    public async Task Register_WhenImportanceLevelIsMisconfigured_ReturnsControlledError()
    {
        Harness harness = CreateHarness(importanceLevel: "Critical");

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(harness.Writer.Committed);
    }

    [Fact]
    public async Task Register_ControlledErrors_DoNotLeakConfiguredRoleId()
    {
        Harness harness = CreateHarness(seedRole: false);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            ConfiguredRoleId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Fallo de persistencia: sin datos parciales ---

    [Fact]
    public async Task Register_WhenPersistenceFails_LeavesNoPartialData()
    {
        Harness harness = CreateHarness();
        harness.Writer.FailureToThrow =
            new InvalidOperationException("fallo simulado de base de datos");

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath,
            ValidRequest(new[] { RestrictionId1, RestrictionId2 }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(harness.Writer.Committed);
        Assert.Null(harness.Writer.CommittedUser);
        Assert.Null(harness.Writer.CommittedUserRole);
        Assert.Empty(harness.Writer.CommittedRestrictions);
    }

    [Fact]
    public async Task Register_WhenPersistenceFails_DoesNotLeakPasswordInResponse()
    {
        Harness harness = CreateHarness();
        harness.Writer.FailureToThrow = new InvalidOperationException(PlainPassword);

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            BasePath, ValidRequest(), TestContext.Current.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(PlainPassword, body, StringComparison.OrdinalIgnoreCase);
    }

    // --- Separación respecto al flujo administrativo ---

    [Fact]
    public async Task AdminUserCreation_DoesNotAssignAnyRole()
    {
        Harness harness = CreateHarness();

        var request = new
        {
            Name = "Carlos",
            LastName = "García",
            Email = $"carlos-{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await harness.Client.PostAsJsonAsync(
            AdminUsersPath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // El flujo administrativo no pasa por el coordinador transaccional móvil
        // y por tanto no asigna automáticamente el rol base.
        Assert.Equal(0, harness.Writer.RegisterCallCount);
        Assert.Null(harness.Writer.CommittedUserRole);
    }

    // --- Fakes ---

    private sealed class Harness
    {
        public FakeUserRepository Users { get; } = new();
        public FakeRoleRepository Roles { get; } = new();
        public FakeRestrictionRepository Restrictions { get; } = new();
        public FakeMobileRegistrationWriter Writer { get; } = new();
        public FakePasswordHasher PasswordHasher { get; } = new();
        public FakeEmailSender EmailSender { get; } = new();
        public HttpClient Client { get; set; } = null!;
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
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || u.Id != excludingId.Value)));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly List<Role> _roles = [];

        public void Seed(Role role) => _roles.Add(role);

        public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken) =>
            Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

        public Task AddAsync(Role role, CancellationToken cancellationToken)
        {
            _roles.Add(role);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRestrictionRepository : IRestrictionRepository
    {
        private readonly List<Restriction> _restrictions = [];

        public void Seed(Restriction restriction) => _restrictions.Add(restriction);

        public Task<Restriction?> GetByIdAsync(
            RestrictionId id, CancellationToken cancellationToken) =>
            Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsByNameAsync(
            string normalizedName,
            RestrictionId? excludingId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_restrictions.Any(r =>
                string.Equals(r.Name, normalizedName, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Restriction restriction, CancellationToken cancellationToken)
        {
            _restrictions.Add(restriction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMobileRegistrationWriter : IMobileRegistrationWriter
    {
        public Exception? FailureToThrow { get; set; }

        public int RegisterCallCount { get; private set; }

        public bool Committed { get; private set; }

        public User? CommittedUser { get; private set; }

        public UserRole? CommittedUserRole { get; private set; }

        public IReadOnlyCollection<UserRestriction> CommittedRestrictions { get; private set; } = [];

        public UserEmailVerification? CommittedEmailVerification { get; private set; }

        public Task RegisterAsync(
            User user,
            UserRole userRole,
            IReadOnlyCollection<UserRestriction> userRestrictions,
            UserEmailVerification emailVerification,
            CancellationToken cancellationToken)
        {
            RegisterCallCount++;

            if (FailureToThrow is not null)
            {
                throw FailureToThrow;
            }

            CommittedUser = user;
            CommittedUserRole = userRole;
            CommittedRestrictions = userRestrictions;
            CommittedEmailVerification = emailVerification;
            Committed = true;

            return Task.CompletedTask;
        }
    }
}
