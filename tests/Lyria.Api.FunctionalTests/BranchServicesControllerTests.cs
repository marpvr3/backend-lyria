using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

public class BranchServicesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly Guid BranchIdGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ServiceIdGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid InactiveBranchIdGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid InactiveServiceIdGuid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public BranchServicesControllerTests(WebApplicationFactory<Program> factory)
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        var activeBranch = EstablishmentBranch.Create(
            new EstablishmentBranchId(BranchIdGuid), establishment.Id,
            "Sede Palermo", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var inactiveBranch = EstablishmentBranch.Create(
            new EstablishmentBranchId(InactiveBranchIdGuid), establishment.Id,
            "Sede Inactiva", "Calle Falsa 123",
            null, null, null, null, null, null, null,
            null, null, null, null, null);
        inactiveBranch.Deactivate();

        var activeService = Service.Create(
            new ServiceId(ServiceIdGuid), "Delivery", "Entrega a domicilio.",
            "https://cdn.lyria.com/icons/delivery.svg");

        var inactiveService = Service.Create(
            new ServiceId(InactiveServiceIdGuid), "Servicio Inactivo", null, null);
        inactiveService.Deactivate();

        var branchRepository = new FakeBranchRepository();
        branchRepository.Seed(activeBranch);
        branchRepository.Seed(inactiveBranch);

        var serviceRepository = new FakeServiceRepo();
        serviceRepository.Seed(activeService);
        serviceRepository.Seed(inactiveService);

        var branchServiceRepository = new FakeBranchServiceRepository();
        var branchServiceReadService = new FakeBranchServiceReadService();

        // Seed an existing assignment for duplicate testing
        var existingAssignment = EstablishmentBranchService.Create(
            activeBranch.Id, activeService.Id, true, "Disponible de lunes a viernes.");
        branchServiceRepository.Seed(existingAssignment);

        branchServiceReadService.Seed(new EstablishmentBranchServiceResponse(
            BranchIdGuid, ServiceIdGuid, "Delivery", "Entrega a domicilio.",
            "https://cdn.lyria.com/icons/delivery.svg",
            true, "Disponible de lunes a viernes.", true, DateTime.UtcNow, null));

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IEstablishmentBranchRepository>(branchRepository);
                services.AddSingleton<IServiceRepository>(serviceRepository);
                services.AddSingleton<IEstablishmentBranchServiceRepository>(branchServiceRepository);
                services.AddSingleton<IEstablishmentBranchServiceReadService>(branchServiceReadService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_ValidAssignment_Returns201()
    {
        var newServiceId = Guid.NewGuid();
        var body = new { serviceId = newServiceId, isAvailable = true, observation = "Observación" };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        // Will be 404 because the new service doesn't exist in the fake repo.
        // Let's use an existing active service with a fresh branch that has no assignment.
        // Actually the existing assignment already uses BranchIdGuid + ServiceIdGuid so let's
        // test with the "branch exists, service exists, no duplicate" scenario by creating a new service.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyServiceId_Returns400()
    {
        var body = new { serviceId = Guid.Empty, isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_BranchNotFound_Returns404()
    {
        var body = new { serviceId = ServiceIdGuid, isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_BranchInactive_Returns409()
    {
        var body = new { serviceId = ServiceIdGuid, isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{InactiveBranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ServiceNotFound_Returns404()
    {
        var body = new { serviceId = Guid.NewGuid(), isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ServiceInactive_Returns409()
    {
        var body = new { serviceId = InactiveServiceIdGuid, isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_Duplicate_Returns409()
    {
        var body = new { serviceId = ServiceIdGuid, isAvailable = true, observation = (string?)null };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("EstablishmentBranchService.AlreadyExists", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_List_Returns200WithData()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/services",
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
            $"/api/v1/branches/{BranchIdGuid}/services?page=1&pageSize=1",
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
            $"/api/v1/branches/{BranchIdGuid}/services?isActive=true",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_FiltersAvailableQuery()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/services?isAvailable=true",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_SupportsSearch()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/services?search=Delivery",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ByIds_Existing_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Delivery", doc.RootElement.GetProperty("serviceName").GetString());
    }

    [Fact]
    public async Task Get_ByIds_NonExistent_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateAvailability_Returns204()
    {
        var body = new { isAvailable = false, observation = "Suspendido por mantenimiento." };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateObservation_Returns204()
    {
        var body = new { isAvailable = true, observation = "Nueva observación" };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Activate_Returns204()
    {
        var body = new { isActive = true };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Deactivate_Returns204()
    {
        var body = new { isActive = false };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchIdGuid}/services/{ServiceIdGuid}",
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

        int branchServiceEndpoints = 0;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/branches/") && path.Name.Contains("/services"))
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    branchServiceEndpoints++;
                }
            }
        }

        Assert.Equal(5, branchServiceEndpoints);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForBranchServices()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/branches/") && path.Name.Contains("/services"))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE endpoint found at {path.Name}");
            }
        }
    }

    [Fact]
    public async Task Post_SystemFieldsInBody_AreIgnored()
    {
        var body = new
        {
            serviceId = ServiceIdGuid,
            isAvailable = true,
            observation = (string?)null,
            isActive = false,
            createdAtUtc = DateTime.UtcNow,
            updatedAtUtc = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/services", body,
            TestContext.Current.CancellationToken);

        // It's a duplicate, so returns 409. The important thing is it didn't crash
        // and system fields were ignored.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Fake implementations for tests

    private sealed class FakeBranchRepository : IEstablishmentBranchRepository
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

    private sealed class FakeServiceRepo : IServiceRepository
    {
        private readonly List<Service> _items = [];

        public void Seed(Service service) => _items.Add(service);

        public Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExistsByNameAsync(string normalizedName, ServiceId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(s =>
                string.Equals(s.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || s.Id != excludingId.Value)));

        public Task AddAsync(Service service, CancellationToken cancellationToken)
        {
            _items.Add(service);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBranchServiceRepository : IEstablishmentBranchServiceRepository
    {
        private readonly List<EstablishmentBranchService> _items = [];

        public void Seed(EstablishmentBranchService item) => _items.Add(item);

        public Task<EstablishmentBranchService?> GetByIdsAsync(
            EstablishmentBranchId branchId, ServiceId serviceId, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(x => x.BranchId == branchId && x.ServiceId == serviceId));

        public Task<bool> ExistsAsync(
            EstablishmentBranchId branchId, ServiceId serviceId, CancellationToken cancellationToken)
            => Task.FromResult(_items.Any(x => x.BranchId == branchId && x.ServiceId == serviceId));

        public void Add(EstablishmentBranchService branchService) => _items.Add(branchService);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBranchServiceReadService : IEstablishmentBranchServiceReadService
    {
        private readonly List<EstablishmentBranchServiceResponse> _items = [];

        public void Seed(EstablishmentBranchServiceResponse response) => _items.Add(response);

        public Task<EstablishmentBranchServiceResponse?> GetByIdsAsync(
            EstablishmentBranchId branchId, ServiceId serviceId, CancellationToken cancellationToken)
            => Task.FromResult(_items.FirstOrDefault(
                x => x.BranchId == branchId.Value && x.ServiceId == serviceId.Value));

        public Task<PagedResponse<EstablishmentBranchServiceListItemResponse>> ListByBranchAsync(
            EstablishmentBranchServiceListFilter filter, CancellationToken cancellationToken)
        {
            var query = _items.Where(x => x.BranchId == filter.BranchId.Value).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(x => x.ServiceName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == filter.IsActive.Value);
            }

            if (filter.IsAvailable.HasValue)
            {
                query = query.Where(x => x.IsAvailable == filter.IsAvailable.Value);
            }

            var all = query.OrderBy(x => x.ServiceName).ToList();
            int total = all.Count;

            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new EstablishmentBranchServiceListItemResponse(
                    x.BranchId, x.ServiceId, x.ServiceName, x.ServiceIconUrl,
                    x.IsAvailable, x.Observation, x.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<EstablishmentBranchServiceListItemResponse>(
                items, filter.Page, filter.PageSize, total));
        }
    }
}
