using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchSchedules.Replace;

public sealed record ReplaceBranchSchedulesCommand(
    Guid BranchId,
    IReadOnlyList<BranchScheduleItem> Schedules) : ICommand;

public sealed record BranchScheduleItem(
    int DayOfWeek,
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight,
    bool IsClosed);
