namespace Lyria.Application.Features.BranchSpecialSchedules;

public sealed record BranchSpecialSchedulesResponse(
    Guid BranchId,
    string From,
    string To,
    IReadOnlyList<BranchSpecialScheduleDateResponse> Schedules);

public sealed record BranchSpecialScheduleDateResponse(
    string Date,
    bool IsClosed,
    string? Reason,
    IReadOnlyList<BranchSpecialScheduleTimeSlotResponse> TimeSlots);

public sealed record BranchSpecialScheduleTimeSlotResponse(
    Guid Id,
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight);
