using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments.Branches;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class PublicEstablishmentsControllerTests
{
    private const string BasePath = "/api/v1/public/establishments";

    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EstablishmentId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EstablishmentId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ServiceId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RestrictionId1 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid BranchId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly HttpClient _client;

    public PublicEstablishmentsControllerTests(WebApplicationFactory<Program> factory)
    {
        var fakeEstablishmentService = new FakePublicEstablishmentReadService();
        var fakeBranchService = new FakePublicBranchReadService();
        var fakeCatalogService = new FakePublicCatalogReadService();

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
                services.AddSingleton<IPublicEstablishmentReadService>(fakeEstablishmentService);
                services.AddSingleton<IPublicBranchReadService>(fakeBranchService);
                services.AddSingleton<IPublicCatalogReadService>(fakeCatalogService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- GET /api/v1/public/establishments (list) ---

    [Fact]
    public async Task List_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithResults()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        JsonElement items = body.GetProperty("items");
        Assert.True(items.GetArrayLength() > 0);
    }

    [Fact]
    public async Task List_ReturnsOk_EmptyList()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?search=nonexistentxyz123", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task List_ReturnsOk_WithSearch()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?search=Let It", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement items = body.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task List_ReturnsOk_WithCategoryFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?categoryId={CategoryId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithCityFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?city=Bogotá", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithServiceFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?serviceId={ServiceId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithRestrictionFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?restrictionId={RestrictionId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithCombinedFilters()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?search=Let&categoryId={CategoryId}&city=Bogotá&serviceId={ServiceId1}&restrictionId={RestrictionId1}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidPage()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?page=0", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidPageSize()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?pageSize=0", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidPageSizeMax()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?pageSize=101", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithComplianceLevelFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?complianceLevel=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithIsCertifiedFilter()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?isCertified=true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidComplianceLevel()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?complianceLevel=99", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WithBranchCountSort()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?sortBy=branchCount", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidSortBy()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?sortBy=invalid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsBadRequest_InvalidSortDirection()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}?sortDirection=invalid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsPagedResponse()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.True(body.TryGetProperty("items", out _));
        Assert.True(body.TryGetProperty("page", out _));
        Assert.True(body.TryGetProperty("pageSize", out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
    }

    // --- GET /api/v1/public/establishments/{slug} ---

    [Fact]
    public async Task GetBySlug_ReturnsOk_ForValidSlug()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/let-it-v", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ReturnsNotFound_ForInvalidSlug()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/non-existent-slug", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ReturnsNotFound_ForInactiveEstablishment()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/inactive-place", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ResponseIncludesBranchesAndSubcollections()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/let-it-v", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.True(body.TryGetProperty("branches", out JsonElement branches));
        Assert.True(branches.GetArrayLength() > 0);

        JsonElement branch = branches[0];
        Assert.True(branch.TryGetProperty("services", out _));
        Assert.True(branch.TryGetProperty("restrictions", out _));
        Assert.True(branch.TryGetProperty("schedules", out _));
        Assert.True(branch.TryGetProperty("images", out _));
        Assert.True(branch.TryGetProperty("address", out _));
        Assert.True(branch.TryGetProperty("contact", out _));
    }

    [Fact]
    public async Task GetBySlug_DoesNotIncludeAdminFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/let-it-v", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.False(body.TryGetProperty("isActive", out _));
        Assert.False(body.TryGetProperty("createdAtUtc", out _));
        Assert.False(body.TryGetProperty("updatedAtUtc", out _));
    }

    // --- Inline fakes ---

    private sealed class FakePublicEstablishmentReadService : IPublicEstablishmentReadService
    {
        private readonly List<PublicEstablishmentListItemResponse> _items;
        private readonly Dictionary<string, PublicEstablishmentDetailResponse> _detailBySlug;

        public FakePublicEstablishmentReadService()
        {
            var category = new PublicCategoryBriefResponse(CategoryId, "Restaurante");
            var services = new List<PublicServiceBriefResponse>
            {
                new(ServiceId1, "Wi-Fi", null)
            };
            var restrictions = new List<PublicRestrictionBriefResponse>
            {
                new(RestrictionId1, "Sin gluten")
            };

            _items =
            [
                new PublicEstablishmentListItemResponse(
                    EstablishmentId1, "Let It V", "let-it-v", "Vegano",
                    null, null, category, 1, ["Bogotá"], services, restrictions),
                new PublicEstablishmentListItemResponse(
                    EstablishmentId2, "Green Bowl", "green-bowl", "Saludable",
                    null, null, category, 2, ["Medellín"], services, restrictions)
            ];

            var categoryDetail = new PublicCategoryDetailResponse(CategoryId, "Restaurante", "Categoría de restaurantes", null);
            var branchAddress = new PublicBranchAddressResponse("Calle 85", "12-34", null, "Chapinero", "Bogotá", "Cundinamarca", "110221", "Colombia");
            var branchLocation = new PublicBranchLocationResponse(4.6689m, -74.0565m);
            var branchContact = new PublicBranchContactResponse("+573001234567", "+573001234567", "info@letitv.com");
            var branchServices = new List<PublicBranchServiceResponse>
            {
                new(ServiceId1, "Wi-Fi", "Internet inalámbrico", null, true, null)
            };
            var branchRestrictions = new List<PublicBranchRestrictionResponse>
            {
                new(RestrictionId1, "Sin gluten", "Opciones sin gluten", 1, "Garantizado", true, null)
            };
            var branchSchedules = new List<PublicBranchDayScheduleResponse>
            {
                new(1, "Lunes", false, [new PublicBranchTimeSlotResponse("08:00", "22:00", false)])
            };
            var branchImages = new List<PublicBranchImageResponse>
            {
                new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "https://img.example.com/1.jpg", "Fachada", true, 1)
            };

            var branchDetail = new PublicBranchDetailResponse(
                BranchId1, "Sede Chapinero", branchAddress, branchLocation, branchContact,
                branchServices, branchRestrictions, branchSchedules, branchImages);

            _detailBySlug = new Dictionary<string, PublicEstablishmentDetailResponse>
            {
                ["let-it-v"] = new(
                    EstablishmentId1, "Let It V", "let-it-v", "Vegano",
                    "https://letitv.com", "@letitv", null, "info@letitv.com", "+573001234567",
                    categoryDetail, [branchDetail])
            };
        }

        public Task<PagedResponse<PublicEstablishmentListItemResponse>> ListAsync(
            PublicEstablishmentListFilter filter, CancellationToken cancellationToken)
        {
            IEnumerable<PublicEstablishmentListItemResponse> query = _items;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(r =>
                    r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(r => r.Category.Id == filter.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(r => r.Cities.Any(c => c.Equals(filter.City, StringComparison.OrdinalIgnoreCase)));
            }

            if (filter.ServiceId.HasValue)
            {
                query = query.Where(r => r.Services.Any(s => s.Id == filter.ServiceId.Value));
            }

            if (filter.RestrictionId.HasValue)
            {
                query = query.Where(r => r.Restrictions.Any(re => re.Id == filter.RestrictionId.Value));
            }

            var all = query.ToList();
            int totalItems = all.Count;
            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return Task.FromResult(new PagedResponse<PublicEstablishmentListItemResponse>(
                items, filter.Page, filter.PageSize, totalItems));
        }

        public Task<PublicEstablishmentDetailResponse?> GetBySlugAsync(
            string normalizedSlug, CancellationToken cancellationToken)
        {
            _detailBySlug.TryGetValue(normalizedSlug, out var result);
            return Task.FromResult(result);
        }
    }

    private sealed class FakePublicBranchReadService : IPublicBranchReadService
    {
        public Task<PublicBranchFullDetailResponse?> GetByIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
            => Task.FromResult<PublicBranchFullDetailResponse?>(null);
    }

    private sealed class FakePublicCatalogReadService : IPublicCatalogReadService
    {
        public Task<PublicCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new PublicCatalogsResponse([], [], [],
                new PublicCatalogLocationsResponse([], [], [])));
    }
}
