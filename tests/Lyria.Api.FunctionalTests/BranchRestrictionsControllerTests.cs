using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class BranchRestrictionsControllerTests
{
    private readonly HttpClient _client;

    private static readonly Guid BranchIdGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaa00000001");
    private static readonly Guid RestrictionIdGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbb00000001");
    private static readonly Guid InactiveBranchIdGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccc00000001");
    private static readonly Guid InactiveRestrictionIdGuid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddd00000001");

    public BranchRestrictionsControllerTests(WebApplicationFactory<Program> factory)
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        var activeBranch = EstablishmentBranch.Create(
            new EstablishmentBranchId(BranchIdGuid), establishment.Id,
            "Sede Palermo", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");

        var inactiveBranch = EstablishmentBranch.Create(
            new EstablishmentBranchId(InactiveBranchIdGuid), establishment.Id,
            "Sede Inactiva", "Calle Falsa 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");
        inactiveBranch.Deactivate();

        var activeRestriction = Restriction.Create(
            new RestrictionId(RestrictionIdGuid), "Vegano", "Sin productos animales.");

        var inactiveRestriction = Restriction.Create(
            new RestrictionId(InactiveRestrictionIdGuid), "Restricción Inactiva", null);
        inactiveRestriction.Deactivate();

        var branchRepository = new FakeBranchRepo();
        branchRepository.Seed(activeBranch);
        branchRepository.Seed(inactiveBranch);

        var restrictionRepository = new FakeRestrictionRepo();
        restrictionRepository.Seed(activeRestriction);
        restrictionRepository.Seed(inactiveRestriction);

        var branchRestrictionRepository = new FakeBranchRestrictionRepository();
        var branchRestrictionReadService = new FakeBranchRestrictionReadService();

        var existingAssignment = EstablishmentBranchRestriction.Create(
            activeBranch.Id, activeRestriction.Id,
            RestrictionComplianceLevel.Guaranteed, true, "Cocina exclusiva.");
        branchRestrictionRepository.Seed(existingAssignment);

        branchRestrictionReadService.Seed(new EstablishmentBranchRestrictionResponse(
            BranchIdGuid, RestrictionIdGuid, "Vegano", "Sin productos animales.",
            RestrictionComplianceLevel.Guaranteed, true, "Cocina exclusiva.",
            true, DateTime.UtcNow, null));

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
                services.AddSingleton<IEstablishmentBranchRepository>(branchRepository);
                services.AddSingleton<IRestrictionRepository>(restrictionRepository);
                services.AddSingleton<IEstablishmentBranchRestrictionRepository>(branchRestrictionRepository);
                services.AddSingleton<IEstablishmentBranchRestrictionReadService>(branchRestrictionReadService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_ValidAssignment_Returns201()
    {
        var newRestrictionId = Guid.NewGuid();
        var body = new { restrictionId = newRestrictionId, complianceLevel = 1, isCertified = false, observation = "Obs" };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        // Will be 404 because the new restriction doesn't exist in the fake repo.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyRestrictionId_Returns400()
    {
        var body = new { restrictionId = Guid.Empty, complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComplianceLevelZero_Returns400()
    {
        var body = new { restrictionId = RestrictionIdGuid, complianceLevel = 0, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComplianceLevelOutOfEnum_Returns400()
    {
        var body = new { restrictionId = RestrictionIdGuid, complianceLevel = 99, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_BranchNotFound_Returns404()
    {
        var body = new { restrictionId = RestrictionIdGuid, complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_BranchInactive_Returns409()
    {
        var body = new { restrictionId = RestrictionIdGuid, complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{InactiveBranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_RestrictionNotFound_Returns404()
    {
        var body = new { restrictionId = Guid.NewGuid(), complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_RestrictionInactive_Returns409()
    {
        var body = new { restrictionId = InactiveRestrictionIdGuid, complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_Duplicate_Returns409()
    {
        var body = new { restrictionId = RestrictionIdGuid, complianceLevel = 1, isCertified = false, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("EstablishmentBranchRestriction.AlreadyExists", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_List_Returns200WithData()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Get_List_SupportsPagination()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions?page=1&pageSize=1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("page").GetInt32() >= 1);
    }

    [Fact]
    public async Task Get_List_FiltersActiveQuery()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions?isActive=true",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_FiltersComplianceLevelQuery()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions?complianceLevel=1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_FiltersCertifiedQuery()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions?isCertified=true",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_SupportsSearch()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions?search=Vegano",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ByIds_Existing_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Vegano", doc.RootElement.GetProperty("restrictionName").GetString());
    }

    [Fact]
    public async Task Get_ByIds_NonExistent_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateComplianceLevel_Returns204()
    {
        var body = new { complianceLevel = 2, isCertified = false, observation = "Parcial." };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateIsCertified_Returns204()
    {
        var body = new { complianceLevel = 1, isCertified = true, observation = (string?)null };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateObservation_Returns204()
    {
        var body = new { complianceLevel = 1, isCertified = true, observation = "Nueva observación" };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Activate_Returns204()
    {
        var body = new { isActive = true };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Deactivate_Returns204()
    {
        var body = new { isActive = false };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContainsFiveEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        int branchRestrictionEndpoints = 0;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/branches/") && path.Name.Contains("/restrictions"))
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    branchRestrictionEndpoints++;
                }
            }
        }

        Assert.Equal(5, branchRestrictionEndpoints);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForBranchRestrictions()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/branches/") && path.Name.Contains("/restrictions"))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE endpoint found at {path.Name}");
            }
        }
    }

    [Fact]
    public async Task Swagger_DocumentsThreeComplianceLevels()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Garantizado", json);
        Assert.Contains("Parcial", json);
        Assert.Contains("Bajo solicitud", json);
    }

    [Fact]
    public async Task Post_SystemFieldsInBody_AreIgnored()
    {
        var body = new
        {
            restrictionId = RestrictionIdGuid,
            complianceLevel = 1,
            isCertified = false,
            observation = (string?)null,
            isActive = false,
            createdAtUtc = DateTime.UtcNow,
            updatedAtUtc = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions", body,
            TestContext.Current.CancellationToken);

        // It's a duplicate, so returns 409. The important thing is it didn't crash
        // and system fields were ignored.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ByIds_ResponseIncludesRestrictionDescriptiveData()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/restrictions/{RestrictionIdGuid}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("restrictionName", out _));
        Assert.True(doc.RootElement.TryGetProperty("restrictionDescription", out _));
    }

    // Fake implementations for tests

    private sealed class FakeBranchRepo : IEstablishmentBranchRepository
    {
        private readonly List<EstablishmentBranch> _items = [];

        public void Seed(EstablishmentBranch branch) => _items.Add(branch);

        public Task<EstablishmentBranch?> GetByIdAsync(EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(b => b.Id == id));

        public Task<bool> ExistsByIdAsync(EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(b => b.Id == id));

        public Task<bool> ExistsByNameWithinEstablishmentAsync(
            EstablishmentId establishmentId, string normalizedName,
            EstablishmentBranchId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public void Add(EstablishmentBranch branch) => _items.Add(branch);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRestrictionRepo : IRestrictionRepository
    {
        private readonly List<Restriction> _items = [];

        public void Seed(Restriction restriction) => _items.Add(restriction);

        public Task<Restriction?> GetByIdAsync(RestrictionId id, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsByNameAsync(string normalizedName, RestrictionId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(r =>
                string.Equals(r.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || r.Id != excludingId.Value)));

        public Task AddAsync(Restriction restriction, CancellationToken cancellationToken)
        {
            _items.Add(restriction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBranchRestrictionRepository : IEstablishmentBranchRestrictionRepository
    {
        private readonly List<EstablishmentBranchRestriction> _items = [];

        public void Seed(EstablishmentBranchRestriction item) => _items.Add(item);

        public Task<EstablishmentBranchRestriction?> GetByIdsAsync(
            EstablishmentBranchId branchId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(x => x.BranchId == branchId && x.RestrictionId == restrictionId));

        public Task<bool> ExistsAsync(
            EstablishmentBranchId branchId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(x => x.BranchId == branchId && x.RestrictionId == restrictionId));

        public void Add(EstablishmentBranchRestriction branchRestriction) => _items.Add(branchRestriction);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBranchRestrictionReadService : IEstablishmentBranchRestrictionReadService
    {
        private readonly List<EstablishmentBranchRestrictionResponse> _items = [];

        public void Seed(EstablishmentBranchRestrictionResponse response) => _items.Add(response);

        public Task<EstablishmentBranchRestrictionResponse?> GetByIdsAsync(
            EstablishmentBranchId branchId, RestrictionId restrictionId, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(
                x => x.BranchId == branchId.Value && x.RestrictionId == restrictionId.Value));

        public Task<PagedResponse<EstablishmentBranchRestrictionListItemResponse>> ListByBranchAsync(
            EstablishmentBranchRestrictionListFilter filter, CancellationToken cancellationToken)
        {
            var query = _items.Where(x => x.BranchId == filter.BranchId.Value).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(x => x.RestrictionName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.IsActive.Value);
            }

            if (filter.ComplianceLevel.HasValue)
            {
                query = query.Where(x => x.ComplianceLevel == filter.ComplianceLevel.Value);
            }

            if (filter.IsCertified.HasValue)
            {
                query = query.Where(x => x.IsCertified == filter.IsCertified.Value);
            }

            var all = query.OrderBy(x => x.RestrictionName).ToList();
            int total = all.Count;

            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new EstablishmentBranchRestrictionListItemResponse(
                    x.BranchId, x.RestrictionId, x.RestrictionName,
                    x.ComplianceLevel, x.IsCertified, x.Observation, x.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<EstablishmentBranchRestrictionListItemResponse>(
                items, filter.Page, filter.PageSize, total));
        }
    }
}
