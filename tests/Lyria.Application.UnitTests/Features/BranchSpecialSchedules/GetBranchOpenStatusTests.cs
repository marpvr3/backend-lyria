using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.BranchSpecialSchedules;
using Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSpecialSchedules;

public sealed class GetBranchOpenStatusTests
{
    private readonly FakeBranchAvailabilityReadService _availabilityReadService = new();
    private readonly FakeTimeZoneService _timeZoneService = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly GetBranchOpenStatusQueryHandler _handler;

    private static readonly Guid BranchId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public GetBranchOpenStatusTests()
    {
        _handler = new GetBranchOpenStatusQueryHandler(
            _availabilityReadService, _timeZoneService, _timeProvider);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero));

        var query = new GetBranchOpenStatusQuery(Guid.NewGuid());

        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_BranchInactive_ReturnsNotFound()
    {
        var date = new DateOnly(2026, 7, 25);
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero));

        var context = new BranchAvailabilityContext(
            BranchId, "America/Argentina/Buenos_Aires", false, null, null);
        _availabilityReadService.SeedContext(context, date);

        var query = new GetBranchOpenStatusQuery(BranchId);
        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_NoSchedule_ReturnsNoScheduleStatus()
    {
        var date = new DateOnly(2026, 7, 25);
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero));

        var context = new BranchAvailabilityContext(
            BranchId, "America/Argentina/Buenos_Aires", true, null, null);
        _availabilityReadService.SeedContext(context, date);

        var query = new GetBranchOpenStatusQuery(BranchId);
        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsOpen);
        Assert.Equal("NoSchedule", result.Value.Status);
    }

    [Fact]
    public async Task Handle_InsideSlot_ReturnsOpen()
    {
        var date = new DateOnly(2026, 7, 25);
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 7, 25, 14, 0, 0, TimeSpan.Zero));

        var currentDaySchedule = new EffectiveSchedule(
            false, null, ScheduleSource.Weekly,
            [new AvailabilityTimeSlot(new TimeOnly(9, 0), new TimeOnly(18, 0), false)]);

        var context = new BranchAvailabilityContext(
            BranchId, "America/Argentina/Buenos_Aires", true, null, currentDaySchedule);
        _availabilityReadService.SeedContext(context, date);

        var query = new GetBranchOpenStatusQuery(BranchId);
        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsOpen);
        Assert.Equal("Open", result.Value.Status);
        Assert.Equal("Abierto", result.Value.StatusName);
    }

    [Fact]
    public async Task Handle_ClosedDay_ReturnsClosed()
    {
        var date = new DateOnly(2026, 12, 25);
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 12, 25, 15, 0, 0, TimeSpan.Zero));

        var currentDaySchedule = new EffectiveSchedule(
            true, "Navidad", ScheduleSource.Special, []);

        var context = new BranchAvailabilityContext(
            BranchId, "America/Argentina/Buenos_Aires", true, null, currentDaySchedule);
        _availabilityReadService.SeedContext(context, date);

        var query = new GetBranchOpenStatusQuery(BranchId);
        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsOpen);
        Assert.Equal("Closed", result.Value.Status);
        Assert.Equal("Cerrado", result.Value.StatusName);
    }

    [Fact]
    public async Task Handle_ResponseContainsTimeZoneId()
    {
        var date = new DateOnly(2026, 7, 25);
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 7, 25, 15, 0, 0, TimeSpan.Zero));

        var context = new BranchAvailabilityContext(
            BranchId, "America/Argentina/Buenos_Aires", true, null, null);
        _availabilityReadService.SeedContext(context, date);

        var query = new GetBranchOpenStatusQuery(BranchId);
        Result<BranchOpenStatusResponse> result =
            await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("America/Argentina/Buenos_Aires", result.Value.TimeZoneId);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
