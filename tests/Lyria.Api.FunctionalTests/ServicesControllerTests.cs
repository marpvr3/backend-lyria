using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Services;
using Lyria.Domain.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

public class ServicesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ServicesControllerTests(WebApplicationFactory<Program> factory)
    {
        var service = Service.Create(
            new ServiceId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            "Delivery", "Entrega a domicilio.", "https://cdn.lyria.com/icons/delivery.svg");

        var readService = new FakeReadService();
        readService.Seed(new ServiceResponse(
            service.Id.Value, service.Name, service.Description, service.IconUrl,
            service.IsActive, DateTime.UtcNow, null));

        var repository = new FakeRepository();
        repository.Seed(service);

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServiceRepository>(repository);
                services.AddSingleton<IServiceReadService>(readService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_ValidService_Returns201()
    {
        var body = new { name = "Wi-Fi", description = "Conexión inalámbrica.", iconUrl = "https://cdn.lyria.com/icons/wifi.svg" };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/services", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        var body = new { name = "", description = (string?)null, iconUrl = (string?)null };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/services", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateName_Returns409()
    {
        var body = new { name = "Delivery", description = (string?)null, iconUrl = (string?)null };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/services", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Service.NameAlreadyExists", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_List_Returns200WithData()
    {
        var response = await _client.GetAsync(
            "/api/v1/services", TestContext.Current.CancellationToken);

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
            "/api/v1/services?page=1&pageSize=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("page").GetInt32() >= 1);
    }

    [Fact]
    public async Task Get_List_SupportsIsActiveFilter()
    {
        var response = await _client.GetAsync(
            "/api/v1/services?isActive=true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_SupportsSearch()
    {
        var response = await _client.GetAsync(
            "/api/v1/services?search=Delivery", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ById_ExistingId_Returns200()
    {
        var response = await _client.GetAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Delivery", root.GetProperty("name").GetString());
        Assert.Equal("Entrega a domicilio.", root.GetProperty("description").GetString());
        Assert.Equal("https://cdn.lyria.com/icons/delivery.svg", root.GetProperty("iconUrl").GetString());
        Assert.True(root.GetProperty("isActive").GetBoolean());
        Assert.True(root.TryGetProperty("createdAtUtc", out _));
        Assert.True(root.TryGetProperty("updatedAtUtc", out _));
    }

    [Fact]
    public async Task Get_ById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/services/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Service.NotFound", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_ValidUpdate_Returns204()
    {
        var body = new { name = "Delivery actualizado", description = "Nueva descripción.", iconUrl = "https://cdn.lyria.com/icons/new.svg" };

        var response = await _client.PutAsJsonAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_DuplicateName_Returns409()
    {
        var createBody = new { name = "Takeaway", description = (string?)null, iconUrl = (string?)null };
        await _client.PostAsJsonAsync("/api/v1/services", createBody, TestContext.Current.CancellationToken);

        var body = new { name = "Takeaway", description = (string?)null, iconUrl = (string?)null };

        var response = await _client.PutAsJsonAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Activate_Returns204()
    {
        var body = new { isActive = true };

        var response = await _client.PatchAsJsonAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Deactivate_Returns204()
    {
        var body = new { isActive = false };

        var response = await _client.PatchAsJsonAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/status",
            body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            "/api/v1/services/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
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
            iconUrl = (string?)null,
            id = Guid.NewGuid(),
            isActive = false,
            createdAtUtc = DateTime.UtcNow,
            updatedAtUtc = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/services", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContainsServiceEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/services", out var servicesPath));
        Assert.True(servicesPath.TryGetProperty("get", out _));
        Assert.True(servicesPath.TryGetProperty("post", out _));

        bool hasServiceById = false;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/services/") && path.Name.Contains("serviceId") && !path.Name.Contains("status"))
            {
                hasServiceById = true;
                Assert.True(path.Value.TryGetProperty("get", out _));
                Assert.True(path.Value.TryGetProperty("put", out _));
                Assert.False(path.Value.TryGetProperty("delete", out _));
            }
        }
        Assert.True(hasServiceById);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForServices()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.StartsWith("/api/v1/services", StringComparison.Ordinal))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE endpoint found at {path.Name}");
            }
        }
    }

    [Fact]
    public async Task Swagger_ContainsServiciosTag()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Servicios", json);
    }

    [Fact]
    public async Task Swagger_IconUrlAppearsInRequestsAndResponses()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("iconUrl", json);
    }

    private sealed class FakeRepository : IServiceRepository
    {
        private readonly List<Service> _services = [];

        public void Seed(Service service) => _services.Add(service);

        public Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken)
            => Task.FromResult(_services.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExistsByNameAsync(string normalizedName, ServiceId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_services.Any(s =>
                string.Equals(s.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || s.Id != excludingId.Value)));

        public Task AddAsync(Service service, CancellationToken cancellationToken)
        {
            _services.Add(service);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeReadService : IServiceReadService
    {
        private readonly List<ServiceResponse> _services = [];

        public void Seed(ServiceResponse response) => _services.Add(response);

        public Task<ServiceResponse?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken)
            => Task.FromResult(_services.FirstOrDefault(s => s.Id == id.Value));

        public Task<PagedResponse<ServiceListItemResponse>> ListAsync(ServiceListFilter filter, CancellationToken cancellationToken)
        {
            var query = _services.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            var all = query.OrderBy(s => s.Name).ToList();
            int total = all.Count;

            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new ServiceListItemResponse(s.Id, s.Name, s.Description, s.IconUrl, s.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<ServiceListItemResponse>(items, filter.Page, filter.PageSize, total));
        }
    }
}
