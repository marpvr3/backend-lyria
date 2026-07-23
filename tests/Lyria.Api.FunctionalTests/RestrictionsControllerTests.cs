
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class RestrictionsControllerTests
{
    private readonly HttpClient _client;

    public RestrictionsControllerTests(WebApplicationFactory<Program> factory)
    {
        var restriction = Restriction.Create(
            new RestrictionId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            "Vegano", "Sin productos de origen animal.");

        var readService = new FakeReadService();
        readService.Seed(new RestrictionResponse(
            restriction.Id.Value, restriction.Name, restriction.Description,
            restriction.IsActive, DateTime.UtcNow, null));

        var repository = new FakeRepository();
        repository.Seed(restriction);

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:WriteTo:1:Args:path"] =
                        Path.Combine(Path.GetTempPath(), "lyria-test-logs", "lyria-.log")
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IRestrictionRepository>(repository);
                services.AddSingleton<IRestrictionReadService>(readService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_ValidRestriction_Returns201()
    {
        var body = new { name = "Sin TACC", description = "Opciones sin gluten." };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/restrictions", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var body = new { name = "", description = (string?)null };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/restrictions", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateName_Returns409()
    {
        var body = new { name = "Vegano", description = (string?)null };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/restrictions", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Restriction.NameAlreadyExists", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_List_Returns200WithData()
    {
        var response = await _client.GetAsync(
            "/api/v1/restrictions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("items").GetArrayLength() > 0);
        Assert.True(root.GetProperty("totalItems").GetInt32() > 0);
    }

    [Fact]
    public async Task Get_List_SupportsPagination()
    {
        var response = await _client.GetAsync(
            "/api/v1/restrictions?page=1&pageSize=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("page").GetInt32() >= 1);
    }

    [Fact]
    public async Task Get_List_SupportsIsActiveFilter()
    {
        var response = await _client.GetAsync(
            "/api/v1/restrictions?isActive=true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_SupportsSearch()
    {
        var response = await _client.GetAsync(
            "/api/v1/restrictions?search=Vegano", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ById_ExistingId_Returns200()
    {
        var response = await _client.GetAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Vegano", root.GetProperty("name").GetString());
        Assert.Equal("Sin productos de origen animal.", root.GetProperty("description").GetString());
        Assert.True(root.GetProperty("isActive").GetBoolean());
        Assert.True(root.TryGetProperty("createdAtUtc", out _));
        Assert.True(root.TryGetProperty("updatedAtUtc", out _));
    }

    [Fact]
    public async Task Get_ById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/restrictions/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Restriction.NotFound", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_ValidUpdate_Returns204()
    {
        var body = new { name = "Vegano actualizado", description = "Nueva descripción." };

        var response = await _client.PutAsJsonAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_DuplicateName_Returns409()
    {
        // Seed another restriction with different name first
        var createBody = new { name = "Sin lactosa", description = (string?)null };
        await _client.PostAsJsonAsync("/api/v1/restrictions", createBody, TestContext.Current.CancellationToken);

        // Try to update the original to the same name
        var body = new { name = "Sin lactosa", description = (string?)null };

        var response = await _client.PutAsJsonAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Activate_Returns204()
    {
        var body = new { isActive = true };

        var response = await _client.PatchAsJsonAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Deactivate_Returns204()
    {
        var body = new { isActive = false };

        var response = await _client.PatchAsJsonAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            "/api/v1/restrictions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Post_SystemFieldsInBody_AreIgnored()
    {
        var body = new
        {
            name = "Con campos extra",
            description = (string?)null,
            id = Guid.NewGuid(),
            isActive = false,
            createdAtUtc = DateTime.UtcNow,
            updatedAtUtc = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/restrictions", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContainsRestrictionEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/restrictions", out var restrictionsPath));
        Assert.True(restrictionsPath.TryGetProperty("get", out _));
        Assert.True(restrictionsPath.TryGetProperty("post", out _));

        // Check individual restriction path exists
        bool hasRestrictionById = false;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/restrictions/") && path.Name.Contains("restrictionId") && !path.Name.Contains("status"))
            {
                hasRestrictionById = true;
                Assert.True(path.Value.TryGetProperty("get", out _));
                Assert.True(path.Value.TryGetProperty("put", out _));
                Assert.False(path.Value.TryGetProperty("delete", out _));
            }
        }
        Assert.True(hasRestrictionById);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForRestrictions()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.StartsWith("/api/v1/restrictions", StringComparison.Ordinal))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE endpoint found at {path.Name}");
            }
        }
    }

    [Fact]
    public async Task Swagger_ContainsRestriccionesTag()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Restricciones", json);
    }

    private sealed class FakeRepository : IRestrictionRepository
    {
        private readonly List<Restriction> _restrictions = [];

        public void Seed(Restriction restriction) => _restrictions.Add(restriction);

        public Task<Restriction?> GetByIdAsync(RestrictionId id, CancellationToken cancellationToken)
            => Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsByNameAsync(string normalizedName, RestrictionId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_restrictions.Any(r =>
                string.Equals(r.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || r.Id != excludingId.Value)));

        public Task AddAsync(Restriction restriction, CancellationToken cancellationToken)
        {
            _restrictions.Add(restriction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeReadService : IRestrictionReadService
    {
        private readonly List<RestrictionResponse> _restrictions = [];

        public void Seed(RestrictionResponse response) => _restrictions.Add(response);

        public Task<RestrictionResponse?> GetByIdAsync(RestrictionId id, CancellationToken cancellationToken)
            => Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<RestrictionListItemResponse>> ListAsync(RestrictionListFilter filter, CancellationToken cancellationToken)
        {
            var query = _restrictions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == filter.IsActive.Value);
            }

            var all = query.OrderBy(r => r.Name).ToList();
            int total = all.Count;

            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new RestrictionListItemResponse(r.Id, r.Name, r.Description, r.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<RestrictionListItemResponse>(items, filter.Page, filter.PageSize, total));
        }
    }
}
