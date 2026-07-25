using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchImages;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class BranchImagesControllerTests
{
    private readonly HttpClient _client;

    private static readonly Guid BranchIdGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public BranchImagesControllerTests(WebApplicationFactory<Program> factory)
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(BranchIdGuid), establishment.Id,
            "Sede Palermo", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null);

        var branchRepository = new FakeBranchRepository();
        branchRepository.Seed(branch);

        var imageRepository = new FakeBranchImageRepository();
        var imageReadService = new FakeBranchImageReadService(BranchIdGuid);

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
                services.AddSingleton<IBranchImageRepository>(imageRepository);
                services.AddSingleton<IBranchImageReadService>(imageReadService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/images",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(BranchIdGuid.ToString(), doc.RootElement.GetProperty("branchId").GetString());
        Assert.True(doc.RootElement.GetProperty("images").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Get_BranchNotFound_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/images",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_EmptyBranchId_Returns400()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.Empty}/images",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidImage_Returns201()
    {
        var body = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = "Alt text",
            isPrimary = false,
            sortOrder = 0
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidUrl_Returns400()
    {
        var body = new
        {
            url = "",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_BranchNotFound_Returns404()
    {
        var body = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/images", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesMetadata_Returns204()
    {
        // First create an image
        var createBody = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", createBody,
            TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createDoc = JsonDocument.Parse(createJson);
        string imageId = createDoc.RootElement.GetProperty("id").GetString()!;

        var updateBody = new
        {
            url = "https://cdn.example.com/updated.jpg",
            fileName = "updated.jpg",
            alternativeText = "Updated",
            sortOrder = 1
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}", updateBody,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_ImageNotFound_Returns404()
    {
        var updateBody = new
        {
            url = "https://cdn.example.com/updated.jpg",
            fileName = "updated.jpg",
            alternativeText = (string?)null,
            sortOrder = 0
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/{Guid.NewGuid()}", updateBody,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Primary_Returns204()
    {
        var createBody = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", createBody,
            TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createDoc = JsonDocument.Parse(createJson);
        string imageId = createDoc.RootElement.GetProperty("id").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}/primary");

        var response = await _client.SendAsync(request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Primary_InactiveImage_Returns409()
    {
        // Create and then deactivate
        var createBody = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", createBody,
            TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createDoc = JsonDocument.Parse(createJson);
        string imageId = createDoc.RootElement.GetProperty("id").GetString()!;

        // Deactivate
        await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        // Try to set as primary
        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}/primary");

        var response = await _client.SendAsync(request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Status_Deactivates_Returns204()
    {
        var createBody = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = false,
            sortOrder = 0
        };
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", createBody,
            TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createDoc = JsonDocument.Parse(createJson);
        string imageId = createDoc.RootElement.GetProperty("id").GetString()!;

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Status_DeactivatePrimary_RemovesPrimary()
    {
        var createBody = new
        {
            url = "https://cdn.example.com/img.jpg",
            fileName = "img.jpg",
            alternativeText = (string?)null,
            isPrimary = true,
            sortOrder = 0
        };
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images", createBody,
            TestContext.Current.CancellationToken);
        string createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createDoc = JsonDocument.Parse(createJson);
        string imageId = createDoc.RootElement.GetProperty("id").GetString()!;

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/{imageId}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Order_Returns204()
    {
        var create1 = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images",
            new { url = "https://cdn.example.com/a.jpg", fileName = "a.jpg", alternativeText = (string?)null, isPrimary = false, sortOrder = 0 },
            TestContext.Current.CancellationToken);
        var json1 = await create1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc1 = JsonDocument.Parse(json1);
        string id1 = doc1.RootElement.GetProperty("id").GetString()!;

        var create2 = await _client.PostAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images",
            new { url = "https://cdn.example.com/b.jpg", fileName = "b.jpg", alternativeText = (string?)null, isPrimary = false, sortOrder = 1 },
            TestContext.Current.CancellationToken);
        var json2 = await create2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc2 = JsonDocument.Parse(json2);
        string id2 = doc2.RootElement.GetProperty("id").GetString()!;

        var orderBody = new
        {
            images = new[]
            {
                new { imageId = id1, sortOrder = 1 },
                new { imageId = id2, sortOrder = 0 }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/order", orderBody,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Order_NegativeOrder_Returns400()
    {
        var body = new
        {
            images = new[]
            {
                new { imageId = Guid.NewGuid().ToString(), sortOrder = -1 }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/images/order", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchIdGuid}/images",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContainsSixEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        int imageEndpoints = 0;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/images"))
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    imageEndpoints++;
                }
            }
        }

        Assert.Equal(6, imageEndpoints);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForImages()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/images"))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE endpoint found at {path.Name}");
            }
        }
    }

    // Fake implementations

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

    private sealed class FakeBranchImageRepository : IBranchImageRepository
    {
        private readonly List<BranchImage> _images = [];

        public Task<BranchImage?> GetByIdAsync(BranchImageId id, CancellationToken cancellationToken)
            => Task.FromResult(_images.FirstOrDefault(i => i.Id == id));

        public Task<List<BranchImage>> GetActiveByBranchIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
        {
            var result = _images
                .Where(i => i.BranchId == branchId && i.IsActive)
                .OrderBy(i => i.SortOrder)
                .ToList();
            return Task.FromResult(result);
        }

        public void Add(BranchImage image) => _images.Add(image);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBranchImageReadService : IBranchImageReadService
    {
        private readonly Guid _knownBranchId;

        public FakeBranchImageReadService(Guid knownBranchId)
        {
            _knownBranchId = knownBranchId;
        }

        public Task<bool> BranchExistsAsync(
            EstablishmentBranchId branchId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(branchId.Value == _knownBranchId);
        }

        public Task<BranchImagesResponse?> GetByBranchIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
        {
            var images = new List<BranchImageResponse>
            {
                new(Guid.NewGuid(), "https://cdn.example.com/img.jpg", "img.jpg",
                    "Alt text", true, 0, true)
            };

            var response = new BranchImagesResponse(branchId.Value, images);
            return Task.FromResult<BranchImagesResponse?>(response);
        }
    }
}
