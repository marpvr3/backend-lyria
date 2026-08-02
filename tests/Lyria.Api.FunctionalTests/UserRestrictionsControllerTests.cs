using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.UserRestrictions;
using Lyria.Application.Features.Users;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class UserRestrictionsControllerTests
{
    private static readonly Guid UserId1 = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
    private static readonly Guid UserWithoutRestrictionsId =
        Guid.Parse("aaaaaaaa-2222-2222-2222-aaaaaaaaaaaa");
    private static readonly Guid NonExistentUserId =
        Guid.Parse("aaaaaaaa-3333-3333-3333-aaaaaaaaaaaa");

    private static readonly Guid AssignedRestrictionId =
        Guid.Parse("bbbbbbbb-1111-1111-1111-bbbbbbbbbbbb");
    private static readonly Guid SecondAssignedRestrictionId =
        Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
    private static readonly Guid UnassignedRestrictionId =
        Guid.Parse("bbbbbbbb-3333-3333-3333-bbbbbbbbbbbb");
    private static readonly Guid NonExistentRestrictionId =
        Guid.Parse("bbbbbbbb-4444-4444-4444-bbbbbbbbbbbb");

    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 2, 5, 0, 0, DateTimeKind.Utc);

    private readonly HttpClient _client;

    private static string BasePath(Guid userId) => $"/api/v1/users/{userId}/restrictions";

    public UserRestrictionsControllerTests(WebApplicationFactory<Program> factory)
    {
        var fakeUserRepository = new FakeUserRepository();
        var fakeRestrictionRepository = new FakeRestrictionRepository();
        var fakeUserRestrictionRepository = new FakeUserRestrictionRepository();
        var fakeUserReadService = new FakeUserReadService();
        var fakeUserRestrictionReadService = new FakeUserRestrictionReadService();

        fakeUserRepository.Seed(User.Create(
            new UserId(UserId1), "Juan", "Pérez", "juan@example.com",
            "hashed_password123", null, null, null));

        fakeUserRepository.Seed(User.Create(
            new UserId(UserWithoutRestrictionsId), "Ana", "López", "ana@example.com",
            "hashed_password123", null, null, null));

        fakeUserReadService.Seed(new UserResponse(
            UserId1, "Juan", "Pérez", "juan@example.com", null, null, null,
            "Unverified", false, null, CreatedAtUtc, null));

        fakeUserReadService.Seed(new UserResponse(
            UserWithoutRestrictionsId, "Ana", "López", "ana@example.com", null, null, null,
            "Unverified", false, null, CreatedAtUtc, null));

        fakeRestrictionRepository.Seed(Restriction.Create(
            new RestrictionId(AssignedRestrictionId), "Sin TACC", null));
        fakeRestrictionRepository.Seed(Restriction.Create(
            new RestrictionId(SecondAssignedRestrictionId), "Sin lactosa", null));
        fakeRestrictionRepository.Seed(Restriction.Create(
            new RestrictionId(UnassignedRestrictionId), "Sin maní", null));

        fakeUserRestrictionRepository.Seed(UserRestriction.Create(
            new UserId(UserId1),
            new RestrictionId(AssignedRestrictionId),
            UserRestrictionImportanceLevels.High,
            CreatedAtUtc));

        fakeUserRestrictionReadService.Seed(new UserRestrictionResponse(
            UserId1, AssignedRestrictionId, "Sin TACC",
            UserRestrictionImportanceLevels.High, CreatedAtUtc));

        fakeUserRestrictionReadService.Seed(new UserRestrictionResponse(
            UserId1, SecondAssignedRestrictionId, "Sin lactosa",
            UserRestrictionImportanceLevels.Low, CreatedAtUtc));

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
                services.AddSingleton<IRestrictionRepository>(fakeRestrictionRepository);
                services.AddSingleton<IUserRestrictionRepository>(fakeUserRestrictionRepository);
                services.AddSingleton<IUserReadService>(fakeUserReadService);
                services.AddSingleton<IUserRestrictionReadService>(fakeUserRestrictionReadService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- POST /api/v1/users/{userId}/restrictions ---

    [Fact]
    public async Task Assign_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            RestrictionId = UnassignedRestrictionId,
            ImportanceLevel = "High"
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
            RestrictionId = UnassignedRestrictionId,
            ImportanceLevel = "Medium"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(
            UnassignedRestrictionId.ToString(),
            response.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Assign_NonExistentUser_ReturnsNotFound()
    {
        var request = new
        {
            RestrictionId = UnassignedRestrictionId,
            ImportanceLevel = "High"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(NonExistentUserId), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("UserRestrictions.UserNotFound", problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Assign_NonExistentRestriction_ReturnsNotFound()
    {
        var request = new
        {
            RestrictionId = NonExistentRestrictionId,
            ImportanceLevel = "High"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(
            "UserRestrictions.RestrictionNotFound",
            problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Assign_DuplicatedAssociation_ReturnsConflict()
    {
        var request = new
        {
            RestrictionId = AssignedRestrictionId,
            ImportanceLevel = "High"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("UserRestrictions.AlreadyExists", problem.Extensions["code"]!.ToString());
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("low")]
    [InlineData("")]
    public async Task Assign_InvalidImportanceLevel_ReturnsBadRequest(string importanceLevel)
    {
        var request = new
        {
            RestrictionId = UnassignedRestrictionId,
            ImportanceLevel = importanceLevel
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_EmptyRestrictionId_ReturnsBadRequest()
    {
        var request = new
        {
            RestrictionId = Guid.Empty,
            ImportanceLevel = "High"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath(UserId1), request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- GET /api/v1/users/{userId}/restrictions ---

    [Fact]
    public async Task GetByUserId_ExistingUser_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserId1), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByUserId_ExistingUser_ReturnsAllRestrictions()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserId1), TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Equal(2, items.Length);
        Assert.Equal(UserId1.ToString(), items[0].GetProperty("userId").GetString());
        Assert.Equal(
            AssignedRestrictionId.ToString(),
            items[0].GetProperty("restrictionId").GetString());
        Assert.Equal("Sin TACC", items[0].GetProperty("restrictionName").GetString());
        Assert.Equal("High", items[0].GetProperty("importanceLevel").GetString());
    }

    [Fact]
    public async Task GetByUserId_UserWithoutRestrictions_ReturnsEmptyList()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(UserWithoutRestrictionsId), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetByUserId_NonExistentUser_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath(NonExistentUserId), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET /api/v1/users/{userId}/restrictions/{restrictionId} ---

    [Fact]
    public async Task GetById_ExistingAssociation_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath(UserId1)}/{AssignedRestrictionId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement item = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(UserId1.ToString(), item.GetProperty("userId").GetString());
        Assert.Equal(
            AssignedRestrictionId.ToString(),
            item.GetProperty("restrictionId").GetString());
        Assert.Equal("Sin TACC", item.GetProperty("restrictionName").GetString());
        Assert.Equal("High", item.GetProperty("importanceLevel").GetString());
    }

    [Fact]
    public async Task GetById_NonExistentAssociation_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath(UserId1)}/{UnassignedRestrictionId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("UserRestrictions.NotFound", problem.Extensions["code"]!.ToString());
    }

    // --- PUT /api/v1/users/{userId}/restrictions/{restrictionId} ---

    [Fact]
    public async Task Update_ValidInput_ReturnsNoContent()
    {
        var request = new { ImportanceLevel = "Medium" };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath(UserId1)}/{AssignedRestrictionId}", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("")]
    public async Task Update_InvalidImportanceLevel_ReturnsBadRequest(string importanceLevel)
    {
        var request = new { ImportanceLevel = importanceLevel };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath(UserId1)}/{AssignedRestrictionId}", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentAssociation_ReturnsNotFound()
    {
        var request = new { ImportanceLevel = "Medium" };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath(UserId1)}/{UnassignedRestrictionId}", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- DELETE /api/v1/users/{userId}/restrictions/{restrictionId} ---

    [Fact]
    public async Task Remove_ExistingAssociation_ReturnsNoContent()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{BasePath(UserId1)}/{AssignedRestrictionId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Remove_NonExistentAssociation_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{BasePath(UserId1)}/{UnassignedRestrictionId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public void Seed(User user) => _users.Add(user);

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<bool> ExistsByEmailAsync(
            string normalizedEmail, UserId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeRestrictionRepository : IRestrictionRepository
    {
        private readonly List<Restriction> _restrictions = [];

        public void Seed(Restriction restriction) => _restrictions.Add(restriction);

        public Task<Restriction?> GetByIdAsync(
            RestrictionId id, CancellationToken cancellationToken)
            => Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsByNameAsync(
            string normalizedName, RestrictionId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Restriction restriction, CancellationToken cancellationToken)
        {
            _restrictions.Add(restriction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUserRestrictionRepository : IUserRestrictionRepository
    {
        private readonly List<UserRestriction> _items = [];

        public void Seed(UserRestriction userRestriction) => _items.Add(userRestriction);

        public Task<UserRestriction?> GetByIdsAsync(
            UserId userId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(
                ur => ur.UserId == userId && ur.RestrictionId == restrictionId));

        public Task<bool> ExistsAsync(
            UserId userId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(
                ur => ur.UserId == userId && ur.RestrictionId == restrictionId));

        public void Add(UserRestriction userRestriction) => _items.Add(userRestriction);

        public void Remove(UserRestriction userRestriction) => _items.Remove(userRestriction);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUserReadService : IUserReadService
    {
        private readonly List<UserResponse> _data = [];

        public void Seed(UserResponse response) => _data.Add(response);

        public Task<UserResponse?> GetByIdAsync(UserId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<UserListItemResponse>> ListAsync(
            UserListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<UserListItemResponse>([], 1, 20, 0));
    }

    private sealed class FakeUserRestrictionReadService : IUserRestrictionReadService
    {
        private readonly List<UserRestrictionResponse> _data = [];

        public void Seed(UserRestrictionResponse response) => _data.Add(response);

        public Task<IReadOnlyList<UserRestrictionResponse>> GetByUserIdAsync(
            UserId userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<UserRestrictionResponse> found = _data
                .Where(r => r.UserId == userId.Value)
                .ToList();

            return Task.FromResult(found);
        }

        public Task<UserRestrictionResponse?> GetByIdsAsync(
            UserId userId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(
                r => r.UserId == userId.Value && r.RestrictionId == restrictionId.Value));
    }
}
