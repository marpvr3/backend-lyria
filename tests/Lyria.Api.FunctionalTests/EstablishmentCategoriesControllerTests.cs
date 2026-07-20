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

public class EstablishmentCategoriesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/v1/establishment-categories";

    private static readonly Guid CategoryId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid InactiveCategoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid NonExistentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly FakeReadService _fakeReadService;
    private readonly HttpClient _client;

    public EstablishmentCategoriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _fakeReadService = new FakeReadService();

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            CategoryId1, "Restaurante", "Establecimientos de comida", null, 1, true));

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            CategoryId2, "Cafetería", null, null, 2, true));

        _fakeReadService.Seed(new EstablishmentCategoryResponse(
            InactiveCategoryId, "Spa", "Inactiva", null, 3, false));

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
                services.AddSingleton<IEstablishmentCategoryReadService>(_fakeReadService);
            });
        });

        _client = configuredFactory.CreateClient();
    }

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

    [Fact]
    public async Task Post_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.PostAsync(
            BasePath, null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.PutAsync(
            $"{BasePath}/{CategoryId1}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Patch_ReturnsMethodNotAllowed()
    {
        HttpRequestMessage request = new(HttpMethod.Patch, $"{BasePath}/{CategoryId1}");

        HttpResponseMessage response = await _client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsMethodNotAllowed()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{BasePath}/{CategoryId1}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

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
}
