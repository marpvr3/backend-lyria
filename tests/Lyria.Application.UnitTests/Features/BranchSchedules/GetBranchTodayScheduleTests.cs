using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.BranchSchedules.GetToday;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSchedules;

public sealed class GetBranchTodayScheduleTests
{
    private readonly FakeBranchScheduleReadService _readService = new();

    private Guid SeedActiveBranch()
    {
        var branchId = Guid.NewGuid();
        _readService.SeedBranch(new EstablishmentBranchId(branchId));
        return branchId;
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetBranchTodayScheduleQueryHandler(
            _readService, fakeTime);

        var query = new GetBranchTodayScheduleQuery(Guid.NewGuid());
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BranchExists_WithSchedule_ReturnsToday()
    {
        var branchId = SeedActiveBranch();

        // 2026-07-23 is a Thursday
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetBranchTodayScheduleQueryHandler(
            _readService, fakeTime);

        var dayResponse = new BranchDayScheduleResponse(4, "Jueves", false,
        [
            new BranchTimeSlotResponse(Guid.NewGuid(), "09:00", "17:00", false)
        ]);
        _readService.SetDayResponse(dayResponse);

        var query = new GetBranchTodayScheduleQuery(branchId);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.DayOfWeek);
        Assert.Equal("Jueves", result.Value.DayName);
    }

    [Fact]
    public async Task Handle_BranchExists_NoScheduleForToday_ReturnsEmptySlots()
    {
        var branchId = SeedActiveBranch();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetBranchTodayScheduleQueryHandler(
            _readService, fakeTime);

        var query = new GetBranchTodayScheduleQuery(branchId);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.TimeSlots);
    }

    [Fact]
    public async Task Handle_UsesTimeProvider_NotDateTimeNow()
    {
        var branchId = SeedActiveBranch();

        // Set to a Monday
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetBranchTodayScheduleQueryHandler(
            _readService, fakeTime);

        var mondayResponse = new BranchDayScheduleResponse(1, "Lunes", false,
        [
            new BranchTimeSlotResponse(Guid.NewGuid(), "08:00", "20:00", false)
        ]);
        _readService.SetDayResponse(mondayResponse);

        var query = new GetBranchTodayScheduleQuery(branchId);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.DayOfWeek);
        Assert.Equal("Lunes", result.Value.DayName);
    }
}
