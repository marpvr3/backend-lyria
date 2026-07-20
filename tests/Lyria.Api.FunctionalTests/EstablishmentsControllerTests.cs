using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Features.Establishments;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

public class EstablishmentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/v1/establishments";

    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EstablishmentId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeRepository _fakeRepository;
    private readonly FakeReadService _fakeReadService;
    private readonly FakeCategoryReadService _fakeCategoryReadService;
    private readonly HttpClient _client;

    public EstablishmentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeRepository = new FakeRepository();
        _fakeReadService = new FakeReadService();
        _fakeCategoryReadService = new FakeCategoryReadService();

        _fakeCategoryReadService.Seed(new EstablishmentCategoryResponse(
            CategoryId, "Restaurante", null, null, 1, true));

        _fakeReadService.Seed(new EstablishmentResponse(
            EstablishmentId1, CategoryId, "Restaurante", "Let It V", "let-it-v",
            "Vegano", "https://letitv.com", "@letitv", null, null, null, false, null, true,
            new DateTime(2026, 7, 15, 5, 0, 0, DateTimeKind.Utc), null));

        WebApplicationFactory<Program> configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LyriaDatabase"] =
                        "Server=(localdb)\\mssqllocaldb;Database=LyriaTest;Trusted_Connection=True"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IEstablishmentRepository>(_fakeRepository);
                services.AddSingleton<IEstablishmentReadService>(_fakeReadService);
                services.AddSingleton<IEstablishmentCategoryReadService>(_fakeCategoryReadService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- GET /api/v1/establishments ---

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

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.True(body.TryGetProperty("items", out _));
        Assert.True(body.TryGetProperty("page", out _));
        Assert.True(body.TryGetProperty("pageSize", out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
    }

    // --- GET /api/v1/establishments/{id} ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{EstablishmentId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingId_SerializesAllFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{EstablishmentId1}", TestContext.Current.CancellationToken);

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.Equal(EstablishmentId1.ToString(), body.GetProperty("id").GetString());
        Assert.Equal("Let It V", body.GetProperty("name").GetString());
        Assert.Equal("let-it-v", body.GetProperty("slug").GetString());
        Assert.Equal("Vegano", body.GetProperty("description").GetString());
        Assert.Equal("https://letitv.com", body.GetProperty("website").GetString());
        Assert.Equal("@letitv", body.GetProperty("instagram").GetString());
        Assert.False(body.GetProperty("isVerified").GetBoolean());
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
        Assert.Equal("Establishments.NotFound", problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task GetById_InvalidGuid_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/not-a-guid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_InvalidGuid_ReturnsValidationProblemDetails()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/not-a-guid", TestContext.Current.CancellationToken);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("id"));
    }

    // --- POST /api/v1/establishments ---

    [Fact]
    public async Task Create_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            CategoryId,
            Name = "Café Zen",
            Slug = "cafe-zen",
            Description = (string?)null,
            Website = (string?)null,
            Instagram = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        var request = new
        {
            CategoryId = Guid.NewGuid(),
            Name = "Test",
            Slug = "test-slug",
            Description = (string?)null,
            Website = (string?)null,
            Instagram = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Audit fields in responses ---

    [Fact]
    public async Task GetById_ExistingId_ContainsAuditFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{EstablishmentId1}", TestContext.Current.CancellationToken);

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.True(body.TryGetProperty("createdAtUtc", out _));
        Assert.True(body.TryGetProperty("updatedAtUtc", out _));
    }

    [Fact]
    public async Task Create_RequestDoesNotAcceptAuditFields()
    {
        var request = new
        {
            CategoryId,
            Name = "Audit Test",
            Slug = "audit-test",
            Description = (string?)null,
            Website = (string?)null,
            Instagram = (string?)null,
            CreatedAtUtc = "2020-01-01T00:00:00Z",
            UpdatedAtUtc = "2020-01-01T00:00:00Z"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        // The extra fields should be ignored — creation should still succeed
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- New fields in responses ---

    [Fact]
    public async Task GetById_ExistingId_ContainsNewFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{EstablishmentId1}", TestContext.Current.CancellationToken);

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.True(body.TryGetProperty("logoUrl", out _));
        Assert.True(body.TryGetProperty("contactEmail", out _));
        Assert.True(body.TryGetProperty("contactPhone", out _));
        Assert.True(body.TryGetProperty("verifiedAtUtc", out _));
    }

    [Fact]
    public async Task Create_RequestDoesNotAcceptVerifiedAtUtc()
    {
        var request = new
        {
            CategoryId,
            Name = "Verificación Test",
            Slug = "verificacion-test",
            Description = (string?)null,
            Website = (string?)null,
            Instagram = (string?)null,
            LogoUrl = (string?)null,
            ContactEmail = (string?)null,
            ContactPhone = (string?)null,
            VerifiedAtUtc = "2026-01-01T00:00:00Z"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        // The VerifiedAtUtc field should be ignored — creation should still succeed
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- DELETE (not allowed) ---

    [Fact]
    public async Task Delete_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{BasePath}/{EstablishmentId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeRepository : IEstablishmentRepository
    {
        private readonly List<Establishment> _establishments = [];

        public Task<Establishment?> GetByIdAsync(
            EstablishmentId id, CancellationToken cancellationToken)
            => Task.FromResult(_establishments.FirstOrDefault(e => e.Id == id));

        public Task<bool> ExistsBySlugAsync(
            string normalizedSlug, EstablishmentId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_establishments.Any(e =>
                e.Slug == normalizedSlug &&
                (excludingId is null || e.Id != excludingId.Value)));

        public Task AddAsync(
            Establishment establishment, CancellationToken cancellationToken)
        {
            _establishments.Add(establishment);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeReadService : IEstablishmentReadService
    {
        private readonly List<EstablishmentResponse> _responses = [];

        public void Seed(EstablishmentResponse response) => _responses.Add(response);

        public Task<EstablishmentResponse?> GetByIdAsync(
            EstablishmentId id, CancellationToken cancellationToken)
            => Task.FromResult(_responses.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
            EstablishmentListFilter filter, CancellationToken cancellationToken)
        {
            IEnumerable<EstablishmentResponse> query = _responses;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(r =>
                    r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    r.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == filter.CategoryId.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == filter.IsActive.Value);
            }

            if (filter.IsVerified.HasValue)
            {
                query = query.Where(r => r.IsVerified == filter.IsVerified.Value);
            }

            var all = query.OrderBy(r => r.Name).ToList();
            int totalItems = all.Count;
            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new EstablishmentListItemResponse(
                    r.Id, r.CategoryId, r.CategoryName, r.Name, r.Slug, r.IsVerified, r.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<EstablishmentListItemResponse>(
                items, filter.Page, filter.PageSize, totalItems));
        }
    }

    private sealed class FakeCategoryReadService : IEstablishmentCategoryReadService
    {
        private readonly List<EstablishmentCategoryResponse> _data = [];

        public void Seed(EstablishmentCategoryResponse response) => _data.Add(response);

        public Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
            EstablishmentCategoryId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value && r.IsActive));

        public Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<EstablishmentCategoryResponse>>(
                _data.Where(r => r.IsActive).OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToList());
    }
}
