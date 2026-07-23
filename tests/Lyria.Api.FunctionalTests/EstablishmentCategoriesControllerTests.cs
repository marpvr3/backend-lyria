using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class EstablishmentCategoriesControllerTests
{
    private const string BasePath = "/api/v1/establishment-categories";

    private static readonly Guid CategoryId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid InactiveCategoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeReadService _fakeReadService;
    private readonly FakeRepository _fakeRepository;
    private readonly HttpClient _client;

    public EstablishmentCategoriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeReadService = new FakeReadService();
        _fakeRepository = new FakeRepository();

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            CategoryId1, "Restaurante", "Establecimientos de comida", null, 1, true));

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            CategoryId2, "Cafetería", null, null, 2, true));

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            InactiveCategoryId, "Spa", "Inactiva", null, 3, false));

        _fakeRepository.Seed(EstablishmentCategory.Create(
            new EstablishmentCategoryId(CategoryId1), "Restaurante", "Establecimientos de comida", null, 1));

        _fakeRepository.Seed(EstablishmentCategory.Create(
            new EstablishmentCategoryId(CategoryId2), "Cafetería", null, null, 2));

        var inactiveCategory = EstablishmentCategory.Create(
            new EstablishmentCategoryId(InactiveCategoryId), "Spa", "Inactiva", null, 3);
        inactiveCategory.Deactivate();
        _fakeRepository.Seed(inactiveCategory);

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
                services.AddSingleton<IEstablishmentCategoryReadService>(_fakeReadService);
                services.AddSingleton<IEstablishmentCategoryRepository>(_fakeRepository);
            });
        });

        _client = configuredFactory.CreateClient();
    }

    // --- GET /api/v1/establishment-categories ---

    [Fact]
    public async Task ListActive_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListActive_SerializesAllFields()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Equal(2, items.Length);

        JsonElement first = items[0];
        Assert.Equal(CategoryId1.ToString(), first.GetProperty("id").GetString());
        Assert.Equal("Restaurante", first.GetProperty("name").GetString());
        Assert.Equal("Establecimientos de comida", first.GetProperty("description").GetString());
        Assert.Equal(1, first.GetProperty("sortOrder").GetInt32());
        Assert.True(first.GetProperty("isActive").GetBoolean());

        JsonElement second = items[1];
        Assert.Equal(CategoryId2.ToString(), second.GetProperty("id").GetString());
        Assert.Null(second.GetProperty("description").GetString());
    }

    [Fact]
    public async Task ListActive_DoesNotContainCodeField()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.False(items[0].TryGetProperty("code", out _));
    }

    [Fact]
    public async Task ListActive_EmptyList_ReturnsOkWithEmptyArray()
    {
        var emptyFake = new FakeReadService();

        WebApplicationFactory<Program> emptyFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
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
                    services.AddSingleton<IEstablishmentCategoryReadService>(emptyFake);
                });
            });

        HttpClient client = emptyFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.Empty(items);
    }

    // --- GET /api/v1/establishment-categories/{id} ---

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{CategoryId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_InactiveCategory_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{InactiveCategoryId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("code"));
        Assert.Equal("EstablishmentCategories.NotFound", problem.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task GetById_InactiveCategory_IsIndistinguishableFromNonExistent()
    {
        HttpResponseMessage inactiveResponse = await _client.GetAsync(
            $"{BasePath}/{InactiveCategoryId}", TestContext.Current.CancellationToken);
        HttpResponseMessage nonExistentResponse = await _client.GetAsync(
            $"{BasePath}/{NonExistentId}", TestContext.Current.CancellationToken);

        Assert.Equal(inactiveResponse.StatusCode, nonExistentResponse.StatusCode);

        ProblemDetails? inactiveProblem = await inactiveResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        ProblemDetails? nonExistentProblem = await nonExistentResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            inactiveProblem!.Extensions["code"]!.ToString(),
            nonExistentProblem!.Extensions["code"]!.ToString());
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
        Assert.Equal("EstablishmentCategories.NotFound", problem.Extensions["code"]!.ToString());
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("id"));
    }

    [Fact]
    public async Task ListActive_ResponseContainsIconUrlField()
    {
        HttpResponseMessage response = await _client.GetAsync(
            BasePath, TestContext.Current.CancellationToken);

        JsonElement[] items = (await response.Content.ReadFromJsonAsync<JsonElement[]>(
            TestContext.Current.CancellationToken))!;

        Assert.True(items[0].TryGetProperty("iconUrl", out _));
    }

    // --- POST /api/v1/establishment-categories ---

    [Fact]
    public async Task Create_ValidInput_ReturnsCreated()
    {
        var request = new
        {
            Name = "Panadería",
            Description = "Venta de pan y productos horneados.",
            SortOrder = 10,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidInput_IncludesLocationHeader()
    {
        var request = new
        {
            Name = "Heladería",
            Description = (string?)null,
            SortOrder = 11,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        string createdId = body.GetProperty("id").GetString()!;
        Assert.True(Guid.TryParse(createdId, out _));

        string expectedSuffix = $"/api/v1/establishment-categories/{createdId}";
        Assert.EndsWith(expectedSuffix, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var request = new
        {
            Name = "",
            Description = (string?)null,
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var request = new
        {
            Name = "Restaurante",
            Description = (string?)null,
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateNameDifferentCasing_ReturnsConflict()
    {
        var request = new
        {
            Name = "restaurante",
            Description = (string?)null,
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_DoesNotAcceptSystemControlledFields()
    {
        var request = new
        {
            Name = "Pizzería",
            Description = (string?)null,
            SortOrder = 12,
            IconUrl = (string?)null,
            Id = Guid.NewGuid(),
            IsActive = false,
            CreatedAtUtc = "2020-01-01T00:00:00Z",
            UpdatedAtUtc = "2020-01-01T00:00:00Z"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            BasePath, request, TestContext.Current.CancellationToken);

        // Extra fields should be ignored — creation should still succeed
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- PUT /api/v1/establishment-categories/{id} ---

    [Fact]
    public async Task Update_ValidInput_ReturnsNoContent()
    {
        var request = new
        {
            Name = "Restaurante",
            Description = "Establecimiento gastronómico dedicado a la preparación de comidas.",
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{CategoryId1}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        var request = new
        {
            Name = "Inexistente",
            Description = (string?)null,
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{NonExistentId}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsConflict()
    {
        var request = new
        {
            Name = "Restaurante",
            Description = (string?)null,
            SortOrder = 1,
            IconUrl = (string?)null
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{CategoryId2}", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_DoesNotAcceptSystemControlledFields()
    {
        var request = new
        {
            Name = "Restaurante",
            Description = "Actualizada.",
            SortOrder = 1,
            IconUrl = (string?)null,
            Id = Guid.NewGuid(),
            IsActive = false,
            CreatedAtUtc = "2020-01-01T00:00:00Z",
            UpdatedAtUtc = "2020-01-01T00:00:00Z"
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{BasePath}/{CategoryId1}", request, TestContext.Current.CancellationToken);

        // Extra fields should be ignored — update should still succeed
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- PATCH /api/v1/establishment-categories/{id}/status ---

    [Fact]
    public async Task UpdateStatus_Deactivate_ReturnsNoContent()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{CategoryId1}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Activate_ReturnsNoContent()
    {
        var request = new { IsActive = true };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{InactiveCategoryId}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentId_ReturnsNotFound()
    {
        var request = new { IsActive = false };

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{BasePath}/{NonExistentId}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_IsIdempotent()
    {
        var request = new { IsActive = true };

        HttpResponseMessage response1 = await _client.PatchAsJsonAsync(
            $"{BasePath}/{CategoryId1}/status", request, TestContext.Current.CancellationToken);
        HttpResponseMessage response2 = await _client.PatchAsJsonAsync(
            $"{BasePath}/{CategoryId1}/status", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
    }

    // --- GET continues working ---

    [Fact]
    public async Task GetById_ContinuesWorking_AfterWriteOperations()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{BasePath}/{CategoryId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement body = (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken));

        Assert.Equal("Restaurante", body.GetProperty("name").GetString());
    }

    // --- DELETE (not allowed) ---

    [Fact]
    public async Task Delete_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{BasePath}/{CategoryId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // --- Inline fakes ---

    private sealed class FakeReadService : IEstablishmentCategoryReadService
    {
        private readonly List<EstablishmentCategoryResponse> _data = [];

        public void Seed(EstablishmentCategoryResponse response) => _data.Add(response);

        public Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
            EstablishmentCategoryId id, CancellationToken cancellationToken)
            => Task.FromResult(_data.FirstOrDefault(r => r.Id == id.Value && r.IsActive));

        public Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<EstablishmentCategoryResponse>>(
                _data.Where(r => r.IsActive)
                    .OrderBy(r => r.SortOrder)
                    .ThenBy(r => r.Name)
                    .ToList());
    }

    private sealed class FakeRepository : IEstablishmentCategoryRepository
    {
        private readonly List<EstablishmentCategory> _categories = [];

        public void Seed(EstablishmentCategory category) => _categories.Add(category);

        public Task<EstablishmentCategory?> GetByIdAsync(
            EstablishmentCategoryId id, CancellationToken cancellationToken)
            => Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));

        public Task<bool> ExistsByNameAsync(
            string normalizedName, EstablishmentCategoryId? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(_categories.Any(c =>
                string.Equals(c.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                (excludingId is null || c.Id != excludingId.Value)));

        public Task AddAsync(
            EstablishmentCategory category, CancellationToken cancellationToken)
        {
            _categories.Add(category);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
