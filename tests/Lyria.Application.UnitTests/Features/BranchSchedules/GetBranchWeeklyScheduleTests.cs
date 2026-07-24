using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.BranchSchedules.GetWeekly;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Application.UnitTests.Features.BranchSchedules;

public sealed class GetBranchWeeklyScheduleTests
{
    private readonly FakeBranchScheduleReadService _readService = new();
    private readonly GetBranchWeeklyScheduleQueryHandler _handler;

    public GetBranchWeeklyScheduleTests()
    {
        _handler = new GetBranchWeeklyScheduleQueryHandler(_readService);
    }

    private Guid SeedActiveBranch()
    {
        var branchId = Guid.NewGuid();
        _readService.SeedBranch(new EstablishmentBranchId(branchId));
        return branchId;
    }

    [Fact]
    public async Task Handle_BranchExists_ReturnsSchedule()
    {
        var branchId = SeedActiveBranch();
        var weeklyResponse = new BranchWeeklyScheduleResponse(branchId,
        [
            new BranchDayScheduleResponse(1, "Lunes", false,
            [
                new BranchTimeSlotResponse(Guid.NewGuid(), "09:00", "17:00", false)
            ])
        ]);
        _readService.SetWeeklyResponse(weeklyResponse);

        var query = new GetBranchWeeklyScheduleQuery(branchId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(branchId, result.Value.BranchId);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ReturnsNotFound()
    {
        var query = new GetBranchWeeklyScheduleQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("EstablishmentBranch.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ScheduleOrderedMondayToSunday()
    {
        var branchId = SeedActiveBranch();
        var days = Enumerable.Range(1, 7)
            .Select(d => new BranchDayScheduleResponse(
                d,
                WeekDayNames.GetSpanishName((WeekDay)d),
                false,
                []))
            .ToList();

        _readService.SetWeeklyResponse(new BranchWeeklyScheduleResponse(branchId, days));

        var query = new GetBranchWeeklyScheduleQuery(branchId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var schedules = result.Value.Schedules;
        for (int i = 0; i < schedules.Count - 1; i++)
        {
            Assert.True(schedules[i].DayOfWeek < schedules[i + 1].DayOfWeek);
        }
    }

    [Fact]
    public async Task Handle_ScheduleSlotsOrderedByOpeningTime()
    {
        var branchId = SeedActiveBranch();
        var mondaySlots = new List<BranchTimeSlotResponse>
        {
            new(Guid.NewGuid(), "12:00", "15:00", false),
            new(Guid.NewGuid(), "19:00", "23:30", false)
        };
        var days = new List<BranchDayScheduleResponse>
        {
            new(1, "Lunes", false, mondaySlots)
        };
        _readService.SetWeeklyResponse(new BranchWeeklyScheduleResponse(branchId, days));

        var query = new GetBranchWeeklyScheduleQuery(branchId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var slots = result.Value.Schedules[0].TimeSlots;
        Assert.Equal("12:00", slots[0].OpeningTime);
        Assert.Equal("19:00", slots[1].OpeningTime);
    }
}
