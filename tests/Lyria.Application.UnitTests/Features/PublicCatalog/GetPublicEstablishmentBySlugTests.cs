using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicEstablishmentBySlugTests
{
    private readonly FakePublicEstablishmentReadService _readService = new();
    private readonly FakeBranchAvailabilityReadService _availabilityReadService = new();
    private readonly FakeTimeZoneService _timeZoneService = new();
    private readonly GetPublicEstablishmentBySlugQueryHandler _handler;

    private static readonly PublicEstablishmentDetailResponse SeedDetail = new(
        Guid.NewGuid(),
        "Alpha Bistro",
        "alpha-bistro",
        "Cocina vegana",
        "https://alpha.com",
        "@alphabistro",
        null,
        "contacto@alpha.com",
        "+57123456789",
        new PublicCategoryDetailResponse(Guid.NewGuid(), "Restaurante", null, null),
        []);

    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    public GetPublicEstablishmentBySlugTests()
    {
        _readService.SeedDetail("alpha-bistro", SeedDetail);
        _handler = new GetPublicEstablishmentBySlugQueryHandler(
            _readService, _availabilityReadService, _timeZoneService,
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task Handle_ReturnsEstablishmentBySlug()
    {
        var query = new GetPublicEstablishmentBySlugQuery("alpha-bistro");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Bistro", result.Value.Name);
        Assert.Equal("alpha-bistro", result.Value.Slug);
    }

    [Fact]
    public async Task Handle_NormalizesSlug()
    {
        // Establishment.NormalizeSlug trims and lowercases
        var query = new GetPublicEstablishmentBySlugQuery("  Alpha-Bistro  ");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Bistro", result.Value.Name);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenNotExists()
    {
        var query = new GetPublicEstablishmentBySlugQuery("non-existent-slug");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("PublicCatalog.EstablishmentNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenInactive()
    {
        // An inactive establishment would not be seeded in the read service
        // (the infrastructure layer filters them out), so querying returns null => NotFound
        var query = new GetPublicEstablishmentBySlugQuery("inactive-establishment");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenCategoryInactive()
    {
        // An establishment with an inactive category would not be returned by the read service
        var query = new GetPublicEstablishmentBySlugQuery("category-inactive-slug");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_EnrichesBranchesWithAvailability()
    {
        var branchId = Guid.NewGuid();
        var detailWithBranch = CreateDetailWithBranches(branchId);
        _readService.SeedDetail("avail-test", detailWithBranch);

        var schedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(17, 0), false)]);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            branchId, "America/Bogota", true, null, schedule));

        var query = new GetPublicEstablishmentBySlugQuery("avail-test");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Branches);
        Assert.NotNull(result.Value.Branches[0].Availability);
        Assert.True(result.Value.Branches[0].Availability!.IsOpen);
    }

    [Fact]
    public async Task Handle_BranchesWithoutContext_HaveNoScheduleAvailability()
    {
        var branchId = Guid.NewGuid();
        var detailWithBranch = CreateDetailWithBranches(branchId);
        _readService.SeedDetail("no-avail-test", detailWithBranch);

        // No availability context seeded → NoSchedule, never null

        var query = new GetPublicEstablishmentBySlugQuery("no-avail-test");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Branches);
        Assert.NotNull(result.Value.Branches[0].Availability);
        Assert.Equal("NoSchedule", result.Value.Branches[0].Availability.Status);
        Assert.Equal("Horario no disponible", result.Value.Branches[0].Availability.StatusName);
        Assert.False(result.Value.Branches[0].Availability.IsOpen);
    }

    [Fact]
    public async Task Handle_MultipleBranches_EachGetAvailability()
    {
        var branchId1 = Guid.NewGuid();
        var branchId2 = Guid.NewGuid();
        var detailWithBranches = CreateDetailWithBranches(branchId1, branchId2);
        _readService.SeedDetail("multi-branch", detailWithBranches);

        var openSchedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(17, 0), false)]);

        var closedSchedule = new EffectiveSchedule(true, "Cerrado temporalmente", ScheduleSource.Special, []);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            branchId1, "America/Bogota", true, null, openSchedule));

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            branchId2, "America/Bogota", true, null, closedSchedule));

        var query = new GetPublicEstablishmentBySlugQuery("multi-branch");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Branches.Count);

        var branch1 = result.Value.Branches.First(b => b.Id == branchId1);
        var branch2 = result.Value.Branches.First(b => b.Id == branchId2);

        Assert.NotNull(branch1.Availability);
        Assert.True(branch1.Availability!.IsOpen);

        Assert.NotNull(branch2.Availability);
        Assert.False(branch2.Availability!.IsOpen);
        Assert.Equal("Closed", branch2.Availability.Status);
    }

    [Fact]
    public async Task Handle_AvailabilityStatusName_IsInSpanish()
    {
        var branchId = Guid.NewGuid();
        var detailWithBranch = CreateDetailWithBranches(branchId);
        _readService.SeedDetail("spanish-status", detailWithBranch);

        var schedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(17, 0), false)]);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            branchId, "America/Bogota", true, null, schedule));

        var query = new GetPublicEstablishmentBySlugQuery("spanish-status");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branches[0].Availability);
        Assert.Equal("Abierto", result.Value.Branches[0].Availability!.StatusName);
    }

    [Fact]
    public async Task Handle_ClosedBranch_HasCorrectAvailability()
    {
        var branchId = Guid.NewGuid();
        var detailWithBranch = CreateDetailWithBranches(branchId);
        _readService.SeedDetail("closed-branch", detailWithBranch);

        var closedSchedule = new EffectiveSchedule(true, "Día festivo", ScheduleSource.Special, []);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            branchId, "America/Bogota", true, null, closedSchedule));

        var query = new GetPublicEstablishmentBySlugQuery("closed-branch");

        Result<PublicEstablishmentDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var closedAvailability = result.Value.Branches[0].Availability;
        Assert.NotNull(closedAvailability);
        Assert.False(closedAvailability!.IsOpen);
        Assert.Equal("Cerrado", closedAvailability.StatusName);
    }

    private static PublicEstablishmentDetailResponse CreateDetailWithBranches(params Guid[] branchIds)
    {
        var branches = branchIds.Select(id => new PublicBranchDetailResponse(
            id,
            $"Sede {id.ToString()[..8]}",
            new PublicBranchAddressResponse("Calle 100", "15", null, "Usaquén", "Bogotá", "Cundinamarca", "110111", "Colombia"),
            new PublicBranchLocationResponse(4.6867m, -74.0465m),
            new PublicBranchContactResponse("+57123456789", null, "sede@test.com"),
            [],
            [],
            [],
            [],
            PublicBranchAvailabilityResponse.Default)).ToList();

        return new PublicEstablishmentDetailResponse(
            Guid.NewGuid(),
            "Test Establishment",
            branchIds.Length == 1 ? "avail-test" : "multi-branch",
            "Descripción de prueba",
            "https://test.com",
            "@test",
            null,
            "contacto@test.com",
            "+57123456789",
            new PublicCategoryDetailResponse(Guid.NewGuid(), "Restaurante", null, null),
            branches);
    }
}
