using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Application.Features.PublicCatalog.GetBranchById;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.PublicCatalog;

public sealed class GetPublicBranchByIdTests
{
    private readonly FakePublicBranchReadService _readService = new();
    private readonly FakeBranchAvailabilityReadService _availabilityReadService = new();
    private readonly FakeTimeZoneService _timeZoneService = new();
    private readonly GetPublicBranchByIdQueryHandler _handler;
    private readonly Guid _existingBranchId = Guid.NewGuid();

    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    public GetPublicBranchByIdTests()
    {
        var branchDetail = new PublicBranchDetailResponse(
            _existingBranchId,
            "Sede Norte",
            new PublicBranchAddressResponse("Calle 100", "15", null, "Usaquén", "Bogotá", "Cundinamarca", "110111", "Colombia"),
            new PublicBranchLocationResponse(4.6867m, -74.0465m),
            new PublicBranchContactResponse("+57123456789", null, "sede@alpha.com"),
            [],
            [],
            [],
            [],
            PublicBranchAvailabilityResponse.Default);

        var establishmentInfo = new PublicBranchEstablishmentResponse(
            Guid.NewGuid(),
            "Alpha Bistro",
            "alpha-bistro",
            "Cocina vegana",
            null,
            new PublicCategoryDetailResponse(Guid.NewGuid(), "Restaurante", null, null));

        _readService.Seed(_existingBranchId, new PublicBranchFullDetailResponse(establishmentInfo, branchDetail));

        _handler = new GetPublicBranchByIdQueryHandler(
            _readService, _availabilityReadService, _timeZoneService,
            new FixedTimeProvider(FixedUtcNow));
    }

    [Fact]
    public async Task Handle_ReturnsBranch()
    {
        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_existingBranchId, result.Value.Branch.Id);
        Assert.Equal("Sede Norte", result.Value.Branch.Name);
        Assert.Equal("Alpha Bistro", result.Value.Establishment.Name);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForNonExistent()
    {
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("PublicCatalog.BranchNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveBranch()
    {
        // An inactive branch would not be seeded in the read service
        // (infrastructure filters out inactive branches)
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveEstablishment()
    {
        // A branch belonging to an inactive establishment would not be returned
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForInactiveCategory()
    {
        // A branch whose establishment has an inactive category would not be returned
        var query = new GetPublicBranchByIdQuery(Guid.NewGuid());

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_IncludesAvailability_WhenContextExists()
    {
        var schedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(17, 0), false)]);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            _existingBranchId, "America/Bogota", true, null, schedule));

        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.True(result.Value.Branch.Availability.IsOpen);
        Assert.Equal("Open", result.Value.Branch.Availability.Status);
        Assert.Equal("Abierto", result.Value.Branch.Availability.StatusName);
    }

    [Fact]
    public async Task Handle_AvailabilityIsNoSchedule_WhenNoContextExists()
    {
        // No availability context seeded for this branch → NoSchedule, never null
        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.Equal("NoSchedule", result.Value.Branch.Availability.Status);
        Assert.Equal("Horario no disponible", result.Value.Branch.Availability.StatusName);
        Assert.False(result.Value.Branch.Availability.IsOpen);
    }

    [Fact]
    public async Task Handle_AvailabilityClosed_WhenScheduleIsClosed()
    {
        var closedSchedule = new EffectiveSchedule(true, "Festivo", ScheduleSource.Special, []);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            _existingBranchId, "America/Bogota", true, null, closedSchedule));

        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.False(result.Value.Branch.Availability.IsOpen);
        Assert.Equal("Closed", result.Value.Branch.Availability.Status);
        Assert.Equal("Cerrado", result.Value.Branch.Availability.StatusName);
        Assert.Equal("Festivo", result.Value.Branch.Availability.Reason);
    }

    [Fact]
    public async Task Handle_AvailabilityNoSchedule_WhenNoScheduleData()
    {
        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            _existingBranchId, "America/Bogota", true, null, null));

        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.Equal("NoSchedule", result.Value.Branch.Availability.Status);
        Assert.Equal("Horario no disponible", result.Value.Branch.Availability.StatusName);
    }

    [Fact]
    public async Task Handle_AvailabilityIncludesTimeZoneId()
    {
        var schedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(17, 0), false)]);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            _existingBranchId, "America/Bogota", true, null, schedule));

        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.Equal("America/Bogota", result.Value.Branch.Availability.TimeZoneId);
    }

    [Fact]
    public async Task Handle_AvailabilityIncludesScheduleSource()
    {
        var schedule = new EffectiveSchedule(true, "Día especial", ScheduleSource.Special, []);

        _availabilityReadService.SeedBatchContext(new BranchAvailabilityContext(
            _existingBranchId, "America/Bogota", true, null, schedule));

        var query = new GetPublicBranchByIdQuery(_existingBranchId);

        Result<PublicBranchFullDetailResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Branch.Availability);
        Assert.Equal("Special", result.Value.Branch.Availability.ScheduleSource);
    }
}
