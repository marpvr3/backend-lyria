namespace Lyria.Application.Features.BranchSpecialSchedules;

public sealed record BranchOpenStatusResponse(
    Guid BranchId,
    bool IsOpen,
    string Status,
    string StatusName,
    string EvaluatedAtUtc,
    string LocalDateTime,
    string TimeZoneId,
    string ScheduleSource,
    string ScheduleDate,
    string? OpensAtLocal,
    string? ClosesAtLocal,
    string? Reason);
