using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class EstablishmentBranchesControllerTests
{
    private static readonly Guid CategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EstablishmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeBranchRepository _fakeBranchRepository;
    private readonly FakeBranchReadService _fakeBranchReadService;
    private readonly FakeEstablishmentRepository _fakeEstablishmentRepository;
    private readonly HttpClient _client;

    public EstablishmentBranchesControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeBranchRepository = new FakeBranchRepository();
        _fakeBranchReadService = new FakeBranchReadService();
        _fakeEstablishmentRepository = new FakeEstablishmentRepository();

        var categoryId = new EstablishmentCategoryId(CategoryId);
        var establishmentId = new EstablishmentId(EstablishmentId);
        var establishment = Establishment.Create(
            establishmentId, categoryId, "Let It V", "let-it-v",
            null, null, null, null, null, null);
        _fakeEstablishmentRepository.Seed(establishment);

        _fakeBranchReadService.Seed(new EstablishmentBranchResponse(
            BranchId1, EstablishmentId, "Sede Principal",
            "Calle 10", "5-20", null, "Centro", "Bogotá", "Cundinamarca",
            "110111", "Colombia", "Calle 10 #5-20, Centro, Bogotá",
            4.6097m, -74.0817m, "+57 1 234 5678", "+57 300 123 4567",
            "sede@letitv.com", 4.5m, 12, true, "America/Argentina/Buenos_Aires",
            new DateTime(2026, 7, 15, 5, 0, 0, DateTimeKind.Utc), null));

        // Seed a domain branch for update/status tests
        var branchId = new EstablishmentBranchId(BranchId1);
        var branch = EstablishmentBranch.Create(
            branchId, establishmentId, "Sede Principal",
            "Calle 10", "5-20", null, "Centro", "Bogotá", "Cundinamarca",
            "110111", "Colombia", 4.6097m, -74.0817m,
            "+57 1 234 5678", "+57 300 123 4567", "sede@letitv.com", "America/Argentina/Buenos_Aires");
        _fakeBranchRepository.Seed(branch);

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
                services.AddSingleton<IEstablishmentBranchRepository>(_fakeBranchRepository);
                services.AddSingleton<IEstablishmentBranchReadService>(_fakeBranchReadService);
                services.AddSingleton<IEstablishmentRepository>(_fakeEstablishmentRepository);
                services.AddSingleton<IEstablishmentCategoryReadService>(
                    new FakeCategoryReadService());
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- POST /api/v1/establishments/{establishmentId}/branches ---

    [Fact]
    public async Task Create_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            Name = "Sede Norte",
            Street = "Calle 100",
            Number = (string?)null,
            AddressComplement = (string?)null,
            Neighborhood = (string?)null,
            City = "Bogotá",
            Province = (string?)null,
            PostalCode = (string?)null,
            Country = (string?)null,
            Latitude = (decimal?)null,
            Longitude = (decimal?)null,
            Phone = (string?)null,
            WhatsApp = (string?)null,
            Email = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            Name = "",
            Street = "",
            Number = (string?)null,
            AddressComplement = (string?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            Province = (string?)null,
            PostalCode = (string?)null,
            Country = (string?)null,
            Latitude = (decimal?)null,
            Longitude = (decimal?)null,
            Phone = (string?)null,
            WhatsApp = (string?)null,
            Email = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_NonExistentEstablishment_ReturnsNotFound()
    {
        var request = new
        {
            Name = "Sede Test",
            Street = "Calle 50",
            Number = (string?)null,
            AddressComplement = (string?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            Province = (string?)null,
            PostalCode = (string?)null,
            Country = (string?)null,
            Latitude = (decimal?)null,
            Longitude = (decimal?)null,
            Phone = (string?)null,
            WhatsApp = (string?)null,
            Email = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/establishments/{NonExistentId}/branches",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var request = new
        {
            Name = "Sede Principal",
            Street = "Calle 20",
            Number = (string?)null,
            AddressComplement = (string?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            Province = (string?)null,
            PostalCode = (string?)null,
            Country = (string?)null,
            Latitude = (decimal?)null,
            Longitude = (decimal?)null,
            Phone = (string?)null,
            WhatsApp = (string?)null,
            Email = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- GET /api/v1/establishments/{establishmentId}/branches ---

    [Fact]
    public async Task List_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsPagedResponse()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            TestContext.Current.CancellationToken);

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.True(body.TryGetProperty("items", out _));
        Assert.True(body.TryGetProperty("page", out _));
        Assert.True(body.TryGetProperty("pageSize", out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
    }

    // --- GET /api/v1/branches/{branchId} ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/branches/{BranchId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/api/v1/branches/{NonExistentId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- PUT /api/v1/branches/{branchId} ---

    [Fact]
    public async Task Update_ValidInput_ReturnsNoContent()
    {
        var request = new
        {
            Name = "Sede Principal Actualizada",
            Street = "Calle 10",
            Number = "5-20",
            AddressComplement = (string?)null,
            Neighborhood = "Centro",
            City = "Bogotá",
            Province = "Cundinamarca",
            PostalCode = "110111",
            Country = "Colombia",
            Latitude = 4.6097m,
            Longitude = -74.0817m,
            Phone = "+57 1 234 5678",
            WhatsApp = "+57 300 123 4567",
            Email = "sede@letitv.com"
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchId1}", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- PATCH /api/v1/branches/{branchId}/status ---

    [Fact]
    public async Task Activate_ReturnsNoContent()
    {
        var request = new { IsActive = true };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchId1}/status", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContent()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchId1}/status", request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- DELETE (not allowed) ---

    [Fact]
    public async Task Delete_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // --- System fields in request ---

    [Fact]
    public async Task Create_RequestDoesNotAcceptSystemFields()
    {
        var request = new
        {
            Name = "Sede Auditoría",
            Street = "Calle 30",
            Number = (string?)null,
            AddressComplement = (string?)null,
            Neighborhood = (string?)null,
            City = (string?)null,
            Province = (string?)null,
            PostalCode = (string?)null,
            Country = (string?)null,
            Latitude = (decimal?)null,
            Longitude = (decimal?)null,
            Phone = (string?)null,
            WhatsApp = (string?)null,
            Email = (string?)null,
            RatingAverage = 5.0m,
            TotalReviews = 100,
            IsActive = false,
            CreatedAtUtc = "2020-01-01T00:00:00Z",
            UpdatedAtUtc = "2020-01-01T00:00:00Z"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/v1/establishments/{EstablishmentId}/branches",
            request, TestContext.Current.CancellationToken);

        // The extra fields should be ignored — creation should still succeed
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeBranchRepository : IEstablishmentBranchRepository
    {
        private readonly List<EstablishmentBranch> _branches = [];

        public void Seed(EstablishmentBranch branch) => _branches.Add(branch);

        public Task<EstablishmentBranch?> GetByIdAsync(
            EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_branches.FirstOrDefault(b => b.Id == id));

        public Task<bool> ExistsByIdAsync(
            EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_branches.Any(b => b.Id == id));

        public Task<bool> ExistsByNameWithinEstablishmentAsync(
            EstablishmentId establishmentId, string normalizedName,
            EstablishmentBranchId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_branches.Any(b =>
                b.EstablishmentId == establishmentId &&
                string.Equals(b.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || b.Id != excludingId.Value)));

        public void Add(EstablishmentBranch branch) => _branches.Add(branch);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeBranchReadService : IEstablishmentBranchReadService
    {
        private readonly List<EstablishmentBranchResponse> _responses = [];

        public void Seed(EstablishmentBranchResponse response) => _responses.Add(response);

        public Task<EstablishmentBranchResponse?> GetByIdAsync(
            EstablishmentBranchId id, CancellationToken cancellationToken)
            => Task.FromResult(_responses.FirstOrDefault(r => r.Id == id.Value));

        public Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
            EstablishmentBranchListFilter filter, CancellationToken cancellationToken)
        {
            IEnumerable<EstablishmentBranchResponse> query =
                _responses.Where(r => r.EstablishmentId == filter.EstablishmentId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim();
                query = query.Where(r =>
                    r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == filter.IsActive.Value);
            }

            var all = query.OrderBy(r => r.Name).ToList();
            int totalItems = all.Count;
            var items = all
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new EstablishmentBranchListItemResponse(
                    r.Id, r.EstablishmentId, r.Name, r.FullAddress,
                    r.City, r.Province, r.Latitude, r.Longitude,
                    r.RatingAverage, r.TotalReviews, r.IsActive))
                .ToList();

            return Task.FromResult(new PagedResponse<EstablishmentBranchListItemResponse>(
                items, filter.Page, filter.PageSize, totalItems));
        }
    }

    private sealed class FakeEstablishmentRepository : IEstablishmentRepository
    {
        private readonly List<Establishment> _establishments = [];

        public void Seed(Establishment establishment) => _establishments.Add(establishment);

        public Task<Establishment?> GetByIdAsync(
            EstablishmentId id, CancellationToken cancellationToken)
            => Task.FromResult(_establishments.FirstOrDefault(e => e.Id == id));

        public Task<bool> ExistsBySlugAsync(
            string normalizedSlug, EstablishmentId? excludingId,
            CancellationToken cancellationToken)
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

    private sealed class FakeCategoryReadService : IEstablishmentCategoryReadService
    {
        public Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
            EstablishmentCategoryId id, CancellationToken cancellationToken)
            => Task.FromResult<EstablishmentCategoryResponse?>(null);

        public Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<EstablishmentCategoryResponse>>([]);
    }
}
