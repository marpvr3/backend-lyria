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
public class PublicBranchesControllerTests
{
    private const string BasePath = "/api/v1/public/branches";

    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EstablishmentId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ServiceId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RestrictionId1 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid InactiveBranchId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly HttpClient _client;

    public PublicBranchesControllerTests(WebApplicationFactory<Program> factory)
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

    // --- GET /api/v1/public/branches/{branchId} ---

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{BranchId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_NonExistent()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{NonExistentId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_Inactive()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{InactiveBranchId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ResponseIncludesConsolidatedData()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{BranchId1}", TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        // Establishment section
        Assert.True(body.TryGetProperty("establishment", out JsonElement establishment));
        Assert.Equal(EstablishmentId1.ToString(), establishment.GetProperty("id").GetString());
        Assert.Equal("Let It V", establishment.GetProperty("name").GetString());
        Assert.Equal("let-it-v", establishment.GetProperty("slug").GetString());
        Assert.True(establishment.TryGetProperty("category", out _));

        // Branch section
        Assert.True(body.TryGetProperty("branch", out JsonElement branch));
        Assert.Equal(BranchId1.ToString(), branch.GetProperty("id").GetString());
        Assert.Equal("Sede Chapinero", branch.GetProperty("name").GetString());
        Assert.True(branch.TryGetProperty("address", out _));
        Assert.True(branch.TryGetProperty("location", out _));
        Assert.True(branch.TryGetProperty("contact", out _));
        Assert.True(branch.TryGetProperty("services", out _));
        Assert.True(branch.TryGetProperty("restrictions", out _));
        Assert.True(branch.TryGetProperty("schedules", out _));
        Assert.True(branch.TryGetProperty("images", out _));
    }

    // --- Inline fakes ---

    private sealed class FakePublicEstablishmentReadService : IPublicEstablishmentReadService
    {
        public Task<PagedResponse<PublicEstablishmentListItemResponse>> ListAsync(
            PublicEstablishmentListFilter filter, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResponse<PublicEstablishmentListItemResponse>(
                [], filter.Page, filter.PageSize, 0));

        public Task<PublicEstablishmentDetailResponse?> GetBySlugAsync(
            string normalizedSlug, CancellationToken cancellationToken)
            => Task.FromResult<PublicEstablishmentDetailResponse?>(null);
    }

    private sealed class FakePublicBranchReadService : IPublicBranchReadService
    {
        private readonly Dictionary<Guid, PublicBranchFullDetailResponse> _data;

        public FakePublicBranchReadService()
        {
            var categoryDetail = new PublicCategoryDetailResponse(CategoryId, "Restaurante", "Categoría de restaurantes", null);
            var establishmentInfo = new PublicBranchEstablishmentResponse(
                EstablishmentId1, "Let It V", "let-it-v", "Vegano", null, categoryDetail);

            var branchAddress = new PublicBranchAddressResponse(
                "Calle 85", "12-34", null, "Chapinero", "Bogotá", "Cundinamarca", "110221", "Colombia");
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
                new(1, "Lunes", false, [new PublicBranchTimeSlotResponse("08:00", "22:00", false)]),
                new(0, "Domingo", true, [])
            };
            var branchImages = new List<PublicBranchImageResponse>
            {
                new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "https://img.example.com/1.jpg", "Fachada", true, 1)
            };

            var branchDetail = new PublicBranchDetailResponse(
                BranchId1, "Sede Chapinero", branchAddress, branchLocation, branchContact,
                branchServices, branchRestrictions, branchSchedules, branchImages);

            _data = new Dictionary<Guid, PublicBranchFullDetailResponse>
            {
                [BranchId1] = new(establishmentInfo, branchDetail)
            };
        }

        public Task<PublicBranchFullDetailResponse?> GetByIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
        {
            _data.TryGetValue(branchId.Value, out var result);
            return Task.FromResult(result);
        }
    }

    private sealed class FakePublicCatalogReadService : IPublicCatalogReadService
    {
        public Task<PublicCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new PublicCatalogsResponse([], [], [],
                new PublicCatalogLocationsResponse([], [], [])));
    }
}
