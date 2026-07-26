using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.BranchSpecialSchedules.UpdateTimeZone;

public sealed record UpdateBranchTimeZoneCommand(
    Guid BranchId,
    string TimeZoneId) : ICommand;
