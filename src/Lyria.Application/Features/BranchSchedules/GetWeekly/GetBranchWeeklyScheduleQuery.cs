using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.BranchSchedules.GetWeekly;

public sealed record GetBranchWeeklyScheduleQuery(Guid BranchId)
    : IQuery<Result<BranchWeeklyScheduleResponse>>;
