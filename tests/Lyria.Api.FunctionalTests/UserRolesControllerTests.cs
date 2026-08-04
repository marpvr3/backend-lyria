using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Users;
using Lyria.Application.Features.UserRoles;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class UserRolesControllerTests
{
    private static readonly Guid UserId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RoleId1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserRoleId1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid NonExistentUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid NonExistentUserRoleId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private readonly HttpClient _client;

    private static string BasePath(Guid userId) => $"/api/v1/users/{userId}/roles";

    public UserRolesControllerTests(WebApplicationFactory<Program> factory)
    {
        var fakeUserRepository = new FakeUserRepository();
        var fakeRoleRepository = new FakeRoleRepository();
        var fakeUserRoleRepository = new FakeUserRoleRepository();
        var fakeUserReadService = new FakeUserReadService();
        var fakeUserRoleReadService = new FakeUserRoleReadService();
        var fakeEstablishmentReadService = new FakeEstablishmentReadService();
        var fakeBranchReadService = new FakeEstablishmentBranchReadService();

        // Seed user
        var user = User.Create(
            new UserId(UserId1), "Juan", "Pérez", "juan@example.com",
            "hashed_password123", "+573001234567", null, null);
        fakeUserRepository.Seed(user);

        fakeUserReadService.Seed(new UserResponse(
            UserId1, "Juan", "Pérez", "juan@example.com",
            "+573001234567", null, null, "Unverified", false, null,
            DateTime.UtcNow, null));

        // Seed role
        var role = Role.Create(
            new RoleId(RoleId1), "Administrador", "Rol de administrador");
        fakeRoleRepository.Seed(role);

        // Seed user role
        var userRole = UserRole.Assign(
            new UserRoleId(UserRoleId1),
            new UserId(UserId1),
            new RoleId(RoleId1),
            ScopeType.Global,
            null,
            null,
            DateTime.UtcNow);
        fakeUserRoleRepository.Seed(userRole);

        fakeUserRoleReadService.Seed(UserId1, new UserRoleResponse(
            UserRoleId1, UserId1, RoleId1, "Administrador",
            "Global", null, null, true, DateTime.UtcNow, null));

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
                services.AddSingleton<IUserRepository>(fakeUserRepository);
                services.AddSingleton<IRoleRepository>(fakeRoleRepository);
                services.AddSingleton<IUserRoleRepository>(fakeUserRoleRepository);
                services.AddSingleton<IUserReadService>(fakeUserReadService);
                services.AddSingleton<IUserRoleReadService>(fakeUserRoleReadService);
                services.AddSingleton<IEstablishmentReadService>(fakeEstablishmentReadService);
                services.AddSingleton<IEstablishmentBranchReadService>(fakeBranchReadService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- GET /api/v1/users/{userId}/roles ---

    [Fact]
    public async Task GetByUserId_ExistingUser_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserId1), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByUserId_ExistingUser_ReturnsRolesList()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserId1), TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Single(items);
        Assert.Equal(UserRoleId1.ToString(), items[0].GetProperty("id").GetString());
        Assert.Equal(RoleId1.ToString(), items[0].GetProperty("roleId").GetString());
        Assert.Equal("Administrador", items[0].GetProperty("roleName").GetString());
        Assert.Equal("Global", items[0].GetProperty("scopeType").GetString());
    }

    [Fact]
    public async Task GetByUserId_ResponseDoesNotContainRoleCode()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserId1), TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Single(items);
        Assert.False(items[0].TryGetProperty("roleCode", out _));
        Assert.True(items[0].TryGetProperty("roleId", out _));
        Assert.True(items[0].TryGetProperty("roleName", out _));
    }

    [Fact]
    public async Task GetByUserId_NonExistentUser_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(NonExistentUserId), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByUserId_NonExistentUser_ReturnsProblemDetailsWithErrorCode()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(NonExistentUserId), TestContext.Current.CancellationToken);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("code"));
        Assert.Equal("UserRoles.UserNotFound", problem.Extensions["code"]!.ToString());
    }

    // --- POST /api/v1/users/{userId}/roles ---

    [Fact]
    public async Task Assign_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            RoleId = RoleId1,
            ScopeType = "Global",
            EstablishmentId = (Guid?)null,
            BranchId = (Guid?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ValidInput_IncludesLocationHeader()
    {
        var request = new
        {
            RoleId = RoleId1,
            ScopeType = "Global",
            EstablishmentId = (Guid?)null,
            BranchId = (Guid?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Assign_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            RoleId = Guid.Empty,
            ScopeType = "",
            EstablishmentId = (Guid?)null,
            BranchId = (Guid?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_NonExistentUser_ReturnsNotFound()
    {
        var request = new
        {
            RoleId = RoleId1,
            ScopeType = "Global",
            EstablishmentId = (Guid?)null,
            BranchId = (Guid?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(NonExistentUserId), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- PATCH /api/v1/users/{userId}/roles/{userRoleId}/active-status ---

    [Fact]
    public async Task SetActiveStatus_Deactivate_ReturnsNoContent()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath(UserId1)}/{UserRoleId1}/active-status", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetActiveStatus_NonExistentUserRole_ReturnsNotFound()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath(UserId1)}/{NonExistentUserRoleId}/active-status", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- PATCH /api/v1/users/{userId}/roles/{userRoleId}/finalize ---

    [Fact]
    public async Task Finalize_ExistingUserRole_ReturnsNoContent()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath(UserId1)}/{UserRoleId1}/finalize", new { },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Finalize_NonExistentUserRole_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath(UserId1)}/{NonExistentUserRoleId}/finalize", new { },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(
            UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

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

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly List<Role> _roles = [];

        public void Seed(Role role) => _roles.Add(role);

        public Task<Role?> GetByIdAsync(
            RoleId id, CancellationToken cancellationToken)
            => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

        public Task AddAsync(
            Role role, CancellationToken cancellationToken)
        {
            _roles.Add(role);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUserRoleRepository : IUserRoleRepository
    {
        private readonly List<UserRole> _userRoles = [];

        public void Seed(UserRole userRole) => _userRoles.Add(userRole);

        public Task<UserRole?> GetByIdAsync(
            UserRoleId id, CancellationToken cancellationToken)
            => Task.FromResult(_userRoles.FirstOrDefault(ur => ur.Id == id));

        public Task AddAsync(
            UserRole userRole, CancellationToken cancellationToken)
        {
            _userRoles.Add(userRole);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUserReadService : IUserReadService
    {
        private readonly List<UserResponse> _data = [];

        public void Seed(UserResponse response) => _data.Add(response);

        public Task<UserResponse?> GetByIdAsync(
            UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<UserListItemResponse>> ListAsync(
            UserListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<UserListItemResponse>([], 1, 20, 0));
    }

    private sealed class FakeUserRoleReadService : IUserRoleReadService
    {
        private readonly Dictionary<Guid, List<UserRoleResponse>> _byUser = [];
        private readonly List<UserRoleResponse> _all = [];

        public void Seed(Guid userId, UserRoleResponse response)
        {
            if (!_byUser.TryGetValue(userId, out List<UserRoleResponse>? list))
            {
                list = [];
                _byUser[userId] = list;
            }

            list.Add(response);
            _all.Add(response);
        }

        public Task<IReadOnlyList<UserRoleResponse>> GetByUserIdAsync(
            UserId userId, CancellationToken cancellationToken)
        {
            if (_byUser.TryGetValue(userId.Value, out List<UserRoleResponse>? roles))
            {
                return Task.FromResult<IReadOnlyList<UserRoleResponse>>(roles);
            }

            return Task.FromResult<IReadOnlyList<UserRoleResponse>>([]);
        }

        public Task<UserRoleResponse?> GetByIdAsync(
            UserRoleId id, CancellationToken cancellationToken)
            => Task.FromResult(_all.FirstOrDefault(r => r.Id == id.Value));
    }

    private sealed class FakeEstablishmentReadService : IEstablishmentReadService
    {
        private readonly List<EstablishmentResponse> _data = [];

        public void Seed(EstablishmentResponse response) => _data.Add(response);

        public Task<EstablishmentResponse?> GetByIdAsync(
            EstablishmentId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
            EstablishmentListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<EstablishmentListItemResponse>([], 1, 20, 0));
    }

    private sealed class FakeEstablishmentBranchReadService : IEstablishmentBranchReadService
    {
        private readonly List<EstablishmentBranchResponse> _data = [];

        public void Seed(EstablishmentBranchResponse response) => _data.Add(response);

        public Task<EstablishmentBranchResponse?> GetByIdAsync(
            EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
            EstablishmentBranchListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<EstablishmentBranchListItemResponse>([], 1, 20, 0));
    }
}

[Collection(LyriaApiTestGroup.Name)]
public class UserRolesBranchScopeControllerTests
{
    private static readonly Guid UserId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RoleId1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid EstablishmentId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherEstablishmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly HttpClient _client;

    public UserRolesBranchScopeControllerTests(WebApplicationFactory<Program> factory)
    {
        var fakeUserRepo = new FakeUserRepository();
        var fakeRoleRepo = new FakeRoleRepository();
        var fakeUserRoleRepo = new FakeUserRoleRepository();
        var fakeUserReadService = new FakeUserReadService();
        var fakeUserRoleReadService = new FakeUserRoleReadService();
        var fakeEstService = new FakeEstablishmentReadService();
        var fakeBranchService = new FakeEstablishmentBranchReadService();

        var user = User.Create(
            new UserId(UserId1), "Ana", "López", "ana@example.com",
            "hashed_pass", null, null, null);
        fakeUserRepo.Seed(user);

        var role = Role.Create(
            new RoleId(RoleId1), "Gerente", null);
        fakeRoleRepo.Seed(role);

        fakeEstService.Seed(new EstablishmentResponse(
            EstablishmentId1, Guid.NewGuid(), "Cat", "EstA", "est-a", null,
            null, null, null, null, null, false, null, true, DateTime.UtcNow, null));

        fakeEstService.Seed(new EstablishmentResponse(
            OtherEstablishmentId, Guid.NewGuid(), "Cat", "EstB", "est-b", null,
            null, null, null, null, null, false, null, true, DateTime.UtcNow, null));

        // Branch belongs to OtherEstablishmentId, NOT EstablishmentId1
        fakeBranchService.Seed(new EstablishmentBranchResponse(
            BranchId1, OtherEstablishmentId, "Sede Norte", "Calle 1", null, null, null,
            null, null, null, null, "Calle 1", null, null, null, null, null,
            0m, 0, true, "America/Bogota", DateTime.UtcNow, null));

        _client = factory.WithWebHostBuilder(builder =>
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
                services.AddSingleton<IUserRepository>(fakeUserRepo);
                services.AddSingleton<IRoleRepository>(fakeRoleRepo);
                services.AddSingleton<IUserRoleRepository>(fakeUserRoleRepo);
                services.AddSingleton<IUserReadService>(fakeUserReadService);
                services.AddSingleton<IUserRoleReadService>(fakeUserRoleReadService);
                services.AddSingleton<IEstablishmentReadService>(fakeEstService);
                services.AddSingleton<IEstablishmentBranchReadService>(fakeBranchService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Assign_BranchScope_BranchDoesNotBelongToEstablishment_ReturnsBadRequest()
    {
        var request = new
        {
            RoleId = RoleId1,
            ScopeType = "Branch",
            EstablishmentId = EstablishmentId1,
            BranchId = BranchId1
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/users/{UserId1}/roles", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.True(problem.Extensions.ContainsKey("code"));
        Assert.Equal("UserRoles.BranchDoesNotBelongToEstablishment",
            problem.Extensions["code"]!.ToString());
    }

    // --- Inline fakes (same as UserRolesControllerTests) ---

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];
        public void Seed(User user) => _users.Add(user);
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        public Task<bool> ExistsByEmailAsync(string email, UserId? excludingId, CancellationToken ct)
            => Task.FromResult(false);
        public Task AddAsync(User user, CancellationToken ct) { _users.Add(user); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly List<Role> _roles = [];
        public void Seed(Role role) => _roles.Add(role);
        public Task<Role?> GetByIdAsync(RoleId id, CancellationToken ct)
            => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));
        public Task AddAsync(Role role, CancellationToken ct) { _roles.Add(role); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUserRoleRepository : IUserRoleRepository
    {
        private readonly List<UserRole> _items = [];
        public Task<UserRole?> GetByIdAsync(UserRoleId id, CancellationToken ct)
            => Task.FromResult(_items.FirstOrDefault(ur => ur.Id == id));
        public Task AddAsync(UserRole ur, CancellationToken ct) { _items.Add(ur); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUserReadService : IUserReadService
    {
        public Task<UserResponse?> GetByIdAsync(UserId id, CancellationToken ct)
            => Task.FromResult<UserResponse?>(null);
        public Task<PagedResponse<UserListItemResponse>> ListAsync(UserListFilter filter, CancellationToken ct)
            => Task.FromResult(new PagedResponse<UserListItemResponse>([], 1, 20, 0));
    }

    private sealed class FakeUserRoleReadService : IUserRoleReadService
    {
        public Task<IReadOnlyList<UserRoleResponse>> GetByUserIdAsync(UserId userId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<UserRoleResponse>>([]);
        public Task<UserRoleResponse?> GetByIdAsync(UserRoleId id, CancellationToken ct)
            => Task.FromResult<UserRoleResponse?>(null);
    }

    private sealed class FakeEstablishmentReadService : IEstablishmentReadService
    {
        private readonly List<EstablishmentResponse> _data = [];
        public void Seed(EstablishmentResponse r) => _data.Add(r);
        public Task<EstablishmentResponse?> GetByIdAsync(EstablishmentId id, CancellationToken ct)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));
        public Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
            EstablishmentListFilter filter, CancellationToken ct)
            => Task.FromResult(new PagedResponse<EstablishmentListItemResponse>([], 1, 20, 0));
    }

    private sealed class FakeEstablishmentBranchReadService : IEstablishmentBranchReadService
    {
        private readonly List<EstablishmentBranchResponse> _data = [];
        public void Seed(EstablishmentBranchResponse r) => _data.Add(r);
        public Task<EstablishmentBranchResponse?> GetByIdAsync(EstablishmentBranchId id, CancellationToken ct)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));
        public Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
            EstablishmentBranchListFilter filter, CancellationToken ct)
            => Task.FromResult(new PagedResponse<EstablishmentBranchListItemResponse>([], 1, 20, 0));
    }
}
