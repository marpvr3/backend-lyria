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
public class PublicFiltersControllerTests
{
    private const string BasePath = "/api/v1/public/catalogs";

    private static readonly Guid CategoryId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ServiceId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RestrictionId1 = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly HttpClient _client;

    public PublicFiltersControllerTests(WebApplicationFactory<Program> factory)
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

    // --- GET /api/v1/public/catalogs ---

    [Fact]
    public async Task GetCatalogs_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCatalogs_ResponseIncludesAllSections()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.True(body.TryGetProperty("categories", out JsonElement categories));
        Assert.True(body.TryGetProperty("services", out JsonElement services));
        Assert.True(body.TryGetProperty("restrictions", out JsonElement restrictions));
        Assert.True(body.TryGetProperty("locations", out JsonElement locations));

        Assert.True(categories.GetArrayLength() > 0);
        Assert.True(services.GetArrayLength() > 0);
        Assert.True(restrictions.GetArrayLength() > 0);

        Assert.True(locations.TryGetProperty("countries", out _));
        Assert.True(locations.TryGetProperty("provinces", out _));
        Assert.True(locations.TryGetProperty("cities", out _));
    }

    [Fact]
    public async Task GetCatalogs_HasCorrectOrder()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        JsonElement categories = body.GetProperty("categories");
        Assert.True(categories.GetArrayLength() >= 2);

        int firstSortOrder = categories[0].GetProperty("sortOrder").GetInt32();
        int secondSortOrder = categories[1].GetProperty("sortOrder").GetInt32();
        Assert.True(firstSortOrder <= secondSortOrder,
            $"Las categorías deben estar ordenadas por sortOrder. Primer elemento: {firstSortOrder}, segundo: {secondSortOrder}.");
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
        public Task<PublicBranchFullDetailResponse?> GetByIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
            => Task.FromResult<PublicBranchFullDetailResponse?>(null);
    }

    private sealed class FakePublicCatalogReadService : IPublicCatalogReadService
    {
        public Task<PublicCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken)
        {
            var categories = new List<PublicCatalogCategoryResponse>
            {
                new(CategoryId1, "Restaurante", null, 1),
                new(CategoryId2, "Cafetería", null, 2)
            };

            var services = new List<PublicCatalogServiceResponse>
            {
                new(ServiceId1, "Wi-Fi", null)
            };

            var restrictions = new List<PublicCatalogRestrictionResponse>
            {
                new(RestrictionId1, "Sin gluten")
            };

            var locations = new PublicCatalogLocationsResponse(
                ["Colombia"],
                [new PublicCatalogProvinceResponse("Colombia", "Cundinamarca")],
                [new PublicCatalogCityResponse("Colombia", "Cundinamarca", "Bogotá")]);

            return Task.FromResult(new PublicCatalogsResponse(categories, services, restrictions, locations));
        }
    }
}
