using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Roles;
using Lyria.Domain.Roles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class RolesControllerTests
{
    private const string BasePath = "/api/v1/roles";

    private static readonly Guid RoleId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RoleId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid InactiveRoleId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeRoleRepository _fakeRepository;
    private readonly FakeRoleReadService _fakeReadService;
    private readonly HttpClient _client;

    public RolesControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeRepository = new FakeRoleRepository();
        _fakeReadService = new FakeRoleReadService();

        _fakeReadService.Seed(new RoleResponse(
            RoleId1, "ADMIN", "Administrador", "Rol de administrador del sistema",
            true, DateTime.UtcNow, null));

        _fakeReadService.Seed(new RoleResponse(
            RoleId2, "OWNER", "Propietario", null,
            true, DateTime.UtcNow, null));

        _fakeReadService.Seed(new RoleResponse(
            InactiveRoleId, "DEPRECATED", "Deprecado", "Rol inactivo",
            false, DateTime.UtcNow, null));

        _fakeReadService.SeedList(new PagedResponse<RoleListItemResponse>(
            [
                new RoleListItemResponse(RoleId1, "ADMIN", "Administrador", true),
                new RoleListItemResponse(RoleId2, "OWNER", "Propietario", true),
                new RoleListItemResponse(InactiveRoleId, "DEPRECATED", "Deprecado", false)
            ],
            Page: 1,
            PageSize: 20,
            TotalItems: 3));

        _fakeRepository.Seed(Role.Create(
            new RoleId(RoleId1), "ADMIN", "Administrador", "Rol de administrador del sistema"));

        _fakeRepository.Seed(Role.Create(
            new RoleId(RoleId2), "OWNER", "Propietario", null));

        var inactiveRole = Role.Create(
            new RoleId(InactiveRoleId), "DEPRECATED", "Deprecado", "Rol inactivo");
        inactiveRole.Deactivate();
        _fakeRepository.Seed(inactiveRole);

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
                services.AddSingleton<IRoleRepository>(_fakeRepository);
                services.AddSingleton<IRoleReadService>(_fakeReadService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- POST /api/v1/roles ---

    [Fact]
    public async Task Create_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            Code = "MANAGER",
            Name = "Gerente",
            Description = "Rol de gerente de establecimiento."
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
            Code = "CASHIER",
            Name = "Cajero",
            Description = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        string createdId = body.GetProperty("id").GetString()!;
        Assert.True(Guid.TryParse(createdId, out _));

        string expectedSuffix = $"/api/v1/roles/{createdId}";
        Assert.EndsWith(expectedSuffix, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            Code = "",
            Name = "",
            Description = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var request = new
        {
            Code = "ADMIN",
            Name = "Otro Admin",
            Description = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- GET /api/v1/roles ---

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
        Assert.Equal(3, items.GetArrayLength());
        Assert.True(body.TryGetProperty("page", out _));
        Assert.True(body.TryGetProperty("pageSize", out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
    }

    // --- GET /api/v1/roles/{id} ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{RoleId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingId_SerializesFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{RoleId1}", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(RoleId1.ToString(), body.GetProperty("id").GetString());
        Assert.Equal("ADMIN", body.GetProperty("code").GetString());
        Assert.Equal("Administrador", body.GetProperty("name").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
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
        Assert.Equal("Roles.NotFound", problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task GetById_InvalidGuid_ReturnsNotFound()
    {
        // El constraint :guid en la ruta rechaza valores no-GUID y ASP.NET Core retorna 404.
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/not-a-guid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- PUT /api/v1/roles/{id} ---

    [Fact]
    public async Task Update_ValidInput_ReturnsNoContent()
    {
        var request = new
        {
            Name = "Super Administrador",
            Description = "Rol con todos los permisos del sistema."
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{RoleId1}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        var request = new
        {
            Name = "Inexistente",
            Description = (string?)null
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
            Description = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{RoleId1}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PATCH /api/v1/roles/{id}/active-status ---

    [Fact]
    public async Task SetActiveStatus_Deactivate_ReturnsNoContent()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{RoleId1}/active-status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetActiveStatus_Activate_ReturnsNoContent()
    {
        var request = new { IsActive = true };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{InactiveRoleId}/active-status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetActiveStatus_NonExistentId_ReturnsNotFound()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{NonExistentId}/active-status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly List<Role> _roles = [];

        public void Seed(Role role) => _roles.Add(role);

        public Task<Role?> GetByIdAsync(
            RoleId id, CancellationToken cancellationToken)
            => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsByCodeAsync(
            string normalizedCode, RoleId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_roles.Any(r =>
                string.Equals(r.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || r.Id != excludingId.Value)));

        public Task AddAsync(
            Role role, CancellationToken cancellationToken)
        {
            _roles.Add(role);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeRoleReadService : IRoleReadService
    {
        private readonly List<RoleResponse> _data = [];
        private PagedResponse<RoleListItemResponse> _pagedResponse =
            new([], 1, 20, 0);

        public void Seed(RoleResponse response) => _data.Add(response);

        public void SeedList(PagedResponse<RoleListItemResponse> response)
            => _pagedResponse = response;

        public Task<RoleResponse?> GetByIdAsync(
            RoleId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<RoleListItemResponse>> ListAsync(
            RoleListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(_pagedResponse);
    }
}
