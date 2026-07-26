using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class BranchSchedulesControllerTests
{
    private readonly HttpClient _client;

    private static readonly Guid BranchIdGuid = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public BranchSchedulesControllerTests(WebApplicationFactory<Program> factory)
    {
        var establishment = Establishment.Create(
            EstablishmentId.New(), EstablishmentCategoryId.New(),
            "Let It V", "let-it-v", null, null, null, null, null, null);

        var branch = EstablishmentBranch.Create(
            new EstablishmentBranchId(BranchIdGuid), establishment.Id,
            "Sede Palermo", "Costa Rica 5865",
            null, null, null, null, null, null, null,
            null, null, null, null, null, "America/Argentina/Buenos_Aires");

        var branchRepository = new FakeBranchRepository();
        branchRepository.Seed(branch);

        var scheduleRepository = new FakeBranchScheduleRepository();
        var scheduleReadService = new FakeBranchScheduleReadService(BranchIdGuid);

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
                services.AddSingleton<IBranchScheduleRepository>(scheduleRepository);
                services.AddSingleton<IBranchScheduleReadService>(scheduleReadService);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Weekly_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(BranchIdGuid.ToString(), doc.RootElement.GetProperty("branchId").GetString());
        Assert.True(doc.RootElement.GetProperty("schedules").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Get_Weekly_ReturnsGroupedData()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules",
            TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var schedules = doc.RootElement.GetProperty("schedules");
        Assert.Equal(7, schedules.GetArrayLength());

        int firstDay = schedules[0].GetProperty("dayOfWeek").GetInt32();
        int lastDay = schedules[schedules.GetArrayLength() - 1].GetProperty("dayOfWeek").GetInt32();
        Assert.Equal(1, firstDay);
        Assert.Equal(7, lastDay);
    }

    [Fact]
    public async Task Get_Weekly_BranchNotFound_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/schedules",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Weekly_EmptyBranchId_Returns400()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.Empty}/schedules",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ValidSchedule_Returns204()
    {
        var body = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 1, openingTime = (string?)"09:00", closingTime = (string?)"17:00", crossesMidnight = false, isClosed = false },
                new { dayOfWeek = 7, openingTime = (string?)null, closingTime = (string?)null, crossesMidnight = false, isClosed = true }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_BranchNotFound_Returns404()
    {
        var body = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 1, openingTime = "09:00", closingTime = "17:00", crossesMidnight = false, isClosed = false }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/schedules", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_InvalidDayOfWeek_Returns400()
    {
        var body = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 0, openingTime = "09:00", closingTime = "17:00", crossesMidnight = false, isClosed = false }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_EmptyBranchId_Returns400()
    {
        var body = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 1, openingTime = "09:00", closingTime = "17:00", crossesMidnight = false, isClosed = false }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{Guid.Empty}/schedules", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReplacesExistingSchedules()
    {
        // First PUT
        var body1 = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 1, openingTime = "09:00", closingTime = "17:00", crossesMidnight = false, isClosed = false }
            }
        };
        await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules", body1,
            TestContext.Current.CancellationToken);

        // Second PUT replaces
        var body2 = new
        {
            schedules = new[]
            {
                new { dayOfWeek = 2, openingTime = "10:00", closingTime = "18:00", crossesMidnight = false, isClosed = false }
            }
        };
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules", body2,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_Today_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules/today",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("dayOfWeek", out _));
        Assert.True(doc.RootElement.TryGetProperty("dayName", out _));
    }

    [Fact]
    public async Task Get_Today_BranchNotFound_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/schedules/today",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns405()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchIdGuid}/schedules",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContainsThreeEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        int scheduleEndpoints = 0;
        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/schedules"))
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    scheduleEndpoints++;
                }
            }
        }

        Assert.Equal(3, scheduleEndpoints);
    }

    [Fact]
    public async Task Swagger_DoesNotContainDeleteForSchedules()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/schedules"))
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

    private sealed class FakeBranchScheduleRepository : IBranchScheduleRepository
    {
        private readonly List<BranchSchedule> _schedules = [];

        public Task<List<BranchSchedule>> GetActiveByBranchIdAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
        {
            var result = _schedules
                .Where(s => s.BranchId == branchId && s.IsActive)
                .ToList();
            return Task.FromResult(result);
        }

        public Task ReplaceSchedulesAsync(
            EstablishmentBranchId branchId,
            IReadOnlyList<BranchSchedule> newSchedules,
            CancellationToken cancellationToken)
        {
            var existing = _schedules.Where(s => s.BranchId == branchId && s.IsActive).ToList();
            foreach (var s in existing)
            {
                s.Deactivate();
            }
            _schedules.AddRange(newSchedules);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBranchScheduleReadService : IBranchScheduleReadService
    {
        private readonly Guid _knownBranchId;

        public FakeBranchScheduleReadService(Guid knownBranchId)
        {
            _knownBranchId = knownBranchId;
        }

        public Task<bool> BranchExistsAsync(
            EstablishmentBranchId branchId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(branchId.Value == _knownBranchId);
        }

        public Task<BranchWeeklyScheduleResponse?> GetWeeklyScheduleAsync(
            EstablishmentBranchId branchId, CancellationToken cancellationToken)
        {
            var days = Enumerable.Range(1, 7)
                .Select(d => new BranchDayScheduleResponse(
                    d,
                    WeekDayNames.GetSpanishName((WeekDay)d),
                    d == 7,
                    d == 7
                        ? []
                        : [new BranchTimeSlotResponse(Guid.NewGuid(), "09:00", "17:00", false)]))
                .ToList();

            var response = new BranchWeeklyScheduleResponse(branchId.Value, days);
            return Task.FromResult<BranchWeeklyScheduleResponse?>(response);
        }

        public Task<BranchDayScheduleResponse?> GetDayScheduleAsync(
            EstablishmentBranchId branchId, WeekDay dayOfWeek, CancellationToken cancellationToken)
        {
            var response = new BranchDayScheduleResponse(
                (int)dayOfWeek,
                WeekDayNames.GetSpanishName(dayOfWeek),
                false,
                [new BranchTimeSlotResponse(Guid.NewGuid(), "09:00", "17:00", false)]);

            return Task.FromResult<BranchDayScheduleResponse?>(response);
        }
    }
}
