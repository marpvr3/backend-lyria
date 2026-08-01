using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lyria.Api.FunctionalTests;

[Collection(LyriaApiTestGroup.Name)]
public class BranchSpecialSchedulesControllerTests
{
    private readonly HttpClient _client;

    private static readonly Guid BranchIdGuid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public BranchSpecialSchedulesControllerTests(WebApplicationFactory<Program> factory)
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

        var specialScheduleRepository = new FakeSpecialScheduleRepository();
        var specialScheduleReadService = new FakeSpecialScheduleReadService(BranchIdGuid);
        var availabilityReadService = new FakeAvailabilityReadService(BranchIdGuid);

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
                services.AddSingleton<IBranchSpecialScheduleRepository>(specialScheduleRepository);
                services.AddSingleton<IBranchSpecialScheduleReadService>(specialScheduleReadService);
                services.AddSingleton<IBranchAvailabilityReadService>(availabilityReadService);
            });
        }).CreateClient();
    }

    // --- GET /api/v1/branches/{branchId}/special-schedules?from=...&to=... ---

    [Fact]
    public async Task GetByRange_ValidRange_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{BranchIdGuid}/special-schedules?from=2026-12-24&to=2026-12-26",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(BranchIdGuid.ToString(), doc.RootElement.GetProperty("branchId").GetString());
    }

    [Fact]
    public async Task GetByRange_BranchNotFound_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/special-schedules?from=2026-12-24&to=2026-12-26",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByRange_EmptyBranchId_Returns400()
    {
        var response = await _client.GetAsync(
            $"/api/v1/branches/{Guid.Empty}/special-schedules?from=2026-12-24&to=2026-12-26",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PUT /api/v1/branches/{branchId}/special-schedules/{date} ---

    [Fact]
    public async Task Replace_ClosedDay_Returns204()
    {
        var body = new
        {
            isClosed = true,
            reason = "Navidad",
            timeSlots = Array.Empty<object>()
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/special-schedules/2026-12-25", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Replace_OpenDayWithSlots_Returns204()
    {
        var body = new
        {
            isClosed = false,
            reason = "Horario especial víspera",
            timeSlots = new[]
            {
                new { openingTime = "09:00", closingTime = "14:00", crossesMidnight = false },
                new { openingTime = "18:00", closingTime = "22:00", crossesMidnight = false }
            }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/special-schedules/2026-12-24", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Replace_BranchNotFound_Returns404()
    {
        var body = new
        {
            isClosed = true,
            reason = (string?)null,
            timeSlots = Array.Empty<object>()
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/special-schedules/2026-12-25", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Replace_EmptyBranchId_Returns400()
    {
        var body = new
        {
            isClosed = true,
            reason = (string?)null,
            timeSlots = Array.Empty<object>()
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/branches/{Guid.Empty}/special-schedules/2026-12-25", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PATCH /api/v1/branches/{branchId}/time-zone ---

    [Fact]
    public async Task UpdateTimeZone_ValidZone_Returns204()
    {
        var body = new { timeZoneId = "America/Bogota" };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/time-zone", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTimeZone_InvalidZone_Returns400()
    {
        var body = new { timeZoneId = "Invalid/Zone" };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{BranchIdGuid}/time-zone", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTimeZone_BranchNotFound_Returns404()
    {
        var body = new { timeZoneId = "America/Bogota" };

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/branches/{Guid.NewGuid()}/time-zone", body,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET /api/v1/public/branches/{branchId}/open-status ---

    [Fact]
    public async Task GetOpenStatus_ValidBranch_Returns200()
    {
        var response = await _client.GetAsync(
            $"/api/v1/public/branches/{BranchIdGuid}/open-status",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(BranchIdGuid.ToString(), doc.RootElement.GetProperty("branchId").GetString());
        Assert.True(doc.RootElement.TryGetProperty("isOpen", out _));
        Assert.True(doc.RootElement.TryGetProperty("status", out _));
        Assert.True(doc.RootElement.TryGetProperty("timeZoneId", out _));
    }

    [Fact]
    public async Task GetOpenStatus_BranchNotFound_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/public/branches/{Guid.NewGuid()}/open-status",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- DELETE not allowed ---

    [Fact]
    public async Task Delete_SpecialSchedules_Returns405()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/branches/{BranchIdGuid}/special-schedules/2026-12-25",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // --- Swagger ---

    [Fact]
    public async Task Swagger_ContainsSpecialScheduleEndpoints()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        bool hasGetByRange = false;
        bool hasPutByDate = false;
        bool hasPatchTimeZone = false;
        bool hasOpenStatus = false;

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/special-schedules") && !path.Name.Contains("{date}"))
            {
                if (path.Value.TryGetProperty("get", out _))
                {
                    hasGetByRange = true;
                }
            }

            if (path.Name.Contains("/special-schedules/{date}"))
            {
                if (path.Value.TryGetProperty("put", out _))
                {
                    hasPutByDate = true;
                }
            }

            if (path.Name.Contains("/time-zone"))
            {
                if (path.Value.TryGetProperty("patch", out _))
                {
                    hasPatchTimeZone = true;
                }
            }

            if (path.Name.Contains("/public/branches/") && path.Name.Contains("/open-status"))
            {
                if (path.Value.TryGetProperty("get", out _))
                {
                    hasOpenStatus = true;
                }
            }
        }

        Assert.True(hasGetByRange, "GET /special-schedules not found in Swagger");
        Assert.True(hasPutByDate, "PUT /special-schedules/{date} not found in Swagger");
        Assert.True(hasPatchTimeZone, "PATCH /time-zone not found in Swagger");
        Assert.True(hasOpenStatus, "GET /public/branches/{branchId}/open-status not found in Swagger");
    }

    [Fact]
    public async Task Swagger_NoDeleteUnderPublic()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Name.Contains("/api/v1/public/"))
            {
                Assert.False(path.Value.TryGetProperty("delete", out _),
                    $"DELETE found under /public at {path.Name}");
                Assert.False(path.Value.TryGetProperty("post", out _),
                    $"POST found under /public at {path.Name}");
                Assert.False(path.Value.TryGetProperty("put", out _),
                    $"PUT found under /public at {path.Name}");
                Assert.False(path.Value.TryGetProperty("patch", out _),
                    $"PATCH found under /public at {path.Name}");
            }
        }
    }

    // --- Fakes ---

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

    private sealed class FakeSpecialScheduleRepository : IBranchSpecialScheduleRepository
    {
        private readonly List<BranchSpecialSchedule> _schedules = [];

        public Task<List<BranchSpecialSchedule>> GetActiveByBranchAndDateAsync(
            EstablishmentBranchId branchId, DateOnly scheduleDate, CancellationToken cancellationToken)
        {
            var result = _schedules
                .Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive)
                .ToList();
            return Task.FromResult(result);
        }

        public Task ReplaceForDateAsync(
            EstablishmentBranchId branchId, DateOnly scheduleDate,
            IReadOnlyList<BranchSpecialSchedule> newSchedules, CancellationToken cancellationToken)
        {
            var existing = _schedules.Where(s => s.BranchId == branchId && s.Date == scheduleDate && s.IsActive).ToList();
            foreach (var s in existing)
            {
                s.Deactivate();
            }
            _schedules.AddRange(newSchedules);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSpecialScheduleReadService : IBranchSpecialScheduleReadService
    {
        private readonly Guid _knownBranchId;

        public FakeSpecialScheduleReadService(Guid knownBranchId) => _knownBranchId = knownBranchId;

        public Task<bool> BranchExistsAsync(EstablishmentBranchId branchId, CancellationToken cancellationToken)
            => Task.FromResult(branchId.Value == _knownBranchId);

        public Task<BranchSpecialSchedulesResponse?> GetByDateRangeAsync(
            EstablishmentBranchId branchId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
        {
            if (branchId.Value != _knownBranchId)
            {
                return Task.FromResult<BranchSpecialSchedulesResponse?>(null);
            }

            var response = new BranchSpecialSchedulesResponse(
                branchId.Value,
                fromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                toDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                []);

            return Task.FromResult<BranchSpecialSchedulesResponse?>(response);
        }
    }

    private sealed class FakeAvailabilityReadService : IBranchAvailabilityReadService
    {
        private readonly Guid _knownBranchId;

        public FakeAvailabilityReadService(Guid knownBranchId) => _knownBranchId = knownBranchId;

        public Task<BranchAvailabilityContext?> GetAvailabilityContextAsync(
            EstablishmentBranchId branchId, DateOnly localDate, CancellationToken cancellationToken)
        {
            if (branchId.Value != _knownBranchId)
            {
                return Task.FromResult<BranchAvailabilityContext?>(null);
            }

            var context = new BranchAvailabilityContext(
                _knownBranchId,
                "America/Argentina/Buenos_Aires",
                true,
                null,
                null);

            return Task.FromResult<BranchAvailabilityContext?>(context);
        }

        public Task<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>
            GetAvailabilityContextsAsync(
                IReadOnlyCollection<EstablishmentBranchId> branchIds,
                DateTimeOffset evaluatedAtUtc,
                ITimeZoneService timeZoneService,
                CancellationToken cancellationToken)
        {
            var result = new Dictionary<EstablishmentBranchId, BranchAvailabilityContext>();

            foreach (var branchId in branchIds)
            {
                if (branchId.Value == _knownBranchId)
                {
                    result[branchId] = new BranchAvailabilityContext(
                        _knownBranchId,
                        "America/Argentina/Buenos_Aires",
                        true,
                        null,
                        null);
                }
            }

            return Task.FromResult<IReadOnlyDictionary<EstablishmentBranchId, BranchAvailabilityContext>>(result);
        }
    }
}
