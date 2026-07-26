using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetByRange;

public sealed record GetBranchSpecialSchedulesQuery(
    Guid BranchId,
    DateOnly From,
    DateOnly To) : IQuery<Result<BranchSpecialSchedulesResponse>>;
