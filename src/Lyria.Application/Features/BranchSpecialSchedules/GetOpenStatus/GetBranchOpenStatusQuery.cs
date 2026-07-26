using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;

public sealed record GetBranchOpenStatusQuery(Guid BranchId)
    : IQuery<Result<BranchOpenStatusResponse>>;
