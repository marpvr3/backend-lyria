using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.BranchSchedules.GetToday;

public sealed record GetBranchTodayScheduleQuery(Guid BranchId)
    : IQuery<Result<BranchDayScheduleResponse>>;
