using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common;
using Lyria.Application.Features.Users;
using Lyria.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class UsersControllerTests
{
    private const string BasePath = "/api/v1/users";

    private static readonly Guid UserId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeUserRepository _fakeRepository;
    private readonly FakeUserReadService _fakeReadService;
    private readonly FakePasswordHasher _fakePasswordHasher;
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeRepository = new FakeUserRepository();
        _fakeReadService = new FakeUserReadService();
        _fakePasswordHasher = new FakePasswordHasher();

        _fakeReadService.Seed(new UserResponse(
            UserId1, "Juan", "Pérez", "juan@example.com",
            "+573001234567", null, null, "Active", true, null,
            DateTime.UtcNow, null));

        _fakeReadService.Seed(new UserResponse(
            UserId2, "María", "López", "maria@example.com",
            null, new DateOnly(1990, 5, 15), null, "Unverified", false, null,
            DateTime.UtcNow, null));

        _fakeReadService.SeedList(new PagedResponse<UserListItemResponse>(
            [
                new UserListItemResponse(UserId1, "Juan", "Pérez", "juan@example.com", "Active", true),
                new UserListItemResponse(UserId2, "María", "López", "maria@example.com", "Unverified", false)
            ],
            Page: 1,
            PageSize: 20,
            TotalItems: 2));

        var user1 = User.Create(
            new UserId(UserId1), "Juan", "Pérez", "juan@example.com",
            "hashed_password123", "+573001234567", null, null);
        user1.ChangeStatus(UserStatus.Active);
        _fakeRepository.Seed(user1);

        var user2 = User.Create(
            new UserId(UserId2), "María", "López", "maria@example.com",
            "hashed_password456", null, new DateOnly(1990, 5, 15), null);
        _fakeRepository.Seed(user2);

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

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IUserRepository>(_fakeRepository);
                services.AddSingleton<IUserReadService>(_fakeReadService);
                services.AddSingleton<IPasswordHasher>(_fakePasswordHasher);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- POST /api/v1/users ---

    [Fact]
    public async Task Create_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            Name = "Carlos",
            LastName = "García",
            Email = "carlos@example.com",
            Password = "SecurePass123!",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidInput_IncludesLocationHeader()
    {
        var request = new
        {
            Name = "Ana",
            LastName = "Martínez",
            Email = "ana@example.com",
            Password = "SecurePass456!",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        string createdId = body.GetProperty("id").GetString()!;
        Assert.True(Guid.TryParse(createdId, out _));

        string expectedSuffix = $"/api/v1/users/{createdId}";
        Assert.EndsWith(expectedSuffix, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            Name = "",
            LastName = "",
            Email = "invalid",
            Password = "",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsConflict()
    {
        var request = new
        {
            Name = "Duplicado",
            LastName = "Test",
            Email = "juan@example.com",
            Password = "SecurePass789!",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- GET /api/v1/users ---

    [Fact]
    public async Task List_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsPagedResponse()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.True(body.TryGetProperty("items", out JsonElement items));
        Assert.Equal(2, items.GetArrayLength());
        Assert.True(body.TryGetProperty("page", out _));
        Assert.True(body.TryGetProperty("pageSize", out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
    }

    // --- GET /api/v1/users/{id} ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{UserId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingId_SerializesFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{UserId1}", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(UserId1.ToString(), body.GetProperty("id").GetString());
        Assert.Equal("Juan", body.GetProperty("name").GetString());
        Assert.Equal("Pérez", body.GetProperty("lastName").GetString());
        Assert.Equal("juan@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{NonExistentId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsProblemDetailsWithErrorCode()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{NonExistentId}", TestContext.Current.CancellationToken);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("code"));
        Assert.Equal("Users.NotFound", problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task GetById_InvalidGuid_ReturnsNotFound()
    {
        // El constraint :guid en la ruta rechaza valores no-GUID y ASP.NET Core retorna 404.
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/not-a-guid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- PUT /api/v1/users/{id} ---

    [Fact]
    public async Task Update_ValidInput_ReturnsNoContent()
    {
        var request = new
        {
            Name = "Juan Carlos",
            LastName = "Pérez",
            Phone = "+573009876543",
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{UserId1}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        var request = new
        {
            Name = "Inexistente",
            LastName = "Usuario",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{NonExistentId}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            Name = "",
            LastName = "",
            Phone = (string?)null,
            BirthDate = (string?)null,
            PhotoUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{UserId1}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PATCH /api/v1/users/{id}/email ---

    [Fact]
    public async Task ChangeEmail_ValidInput_ReturnsNoContent()
    {
        var request = new { Email = "nuevoemail@example.com" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/email", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_NonExistentId_ReturnsNotFound()
    {
        var request = new { Email = "test@example.com" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{NonExistentId}/email", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_InvalidInput_ReturnsBadRequest()
    {
        var request = new { Email = "" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/email", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_DuplicateEmail_ReturnsConflict()
    {
        var request = new { Email = "maria@example.com" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/email", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- PATCH /api/v1/users/{id}/password ---

    [Fact]
    public async Task ChangePassword_ValidInput_ReturnsNoContent()
    {
        var request = new { Password = "NewSecurePass123!" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/password", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NonExistentId_ReturnsNotFound()
    {
        var request = new { Password = "NewSecurePass123!" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{NonExistentId}/password", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_InvalidInput_ReturnsBadRequest()
    {
        var request = new { Password = "" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/password", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PATCH /api/v1/users/{id}/status ---

    [Fact]
    public async Task ChangeStatus_ValidTransition_ReturnsNoContent()
    {
        var request = new { Status = "Active" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId2}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_NonExistentId_ReturnsNotFound()
    {
        var request = new { Status = "Active" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{NonExistentId}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_InvalidInput_ReturnsBadRequest()
    {
        var request = new { Status = "" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{UserId1}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(
            UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(
            string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByEmailAsync(
            string normalizedEmail, UserId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_users.Any(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || u.Id != excludingId.Value)));

        public Task AddAsync(
            User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUserReadService : IUserReadService
    {
        private readonly List<UserResponse> _data = [];
        private PagedResponse<UserListItemResponse> _pagedResponse =
            new([], 1, 20, 0);

        public void Seed(UserResponse response) => _data.Add(response);

        public void SeedList(PagedResponse<UserListItemResponse> response)
            => _pagedResponse = response;

        public Task<UserResponse?> GetByIdAsync(
            UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<UserListItemResponse>> ListAsync(
            UserListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(_pagedResponse);
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
}
