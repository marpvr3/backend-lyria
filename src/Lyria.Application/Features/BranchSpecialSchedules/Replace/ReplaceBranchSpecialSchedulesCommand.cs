using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchSpecialSchedules.Replace;

public sealed record ReplaceBranchSpecialSchedulesCommand(
    Guid BranchId,
    DateOnly Date,
    bool IsClosed,
    string? Reason,
    IReadOnlyList<SpecialScheduleTimeSlotItem> TimeSlots) : ICommand;

public sealed record SpecialScheduleTimeSlotItem(
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight);
