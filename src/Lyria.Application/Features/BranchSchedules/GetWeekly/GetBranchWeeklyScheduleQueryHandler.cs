using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSchedules.GetWeekly;

public sealed class GetBranchWeeklyScheduleQueryHandler(
    IBranchScheduleReadService readService)
    : IQueryHandler<GetBranchWeeklyScheduleQuery, Result<BranchWeeklyScheduleResponse>>
{
    public async ValueTask<Result<BranchWeeklyScheduleResponse>> Handle(
        GetBranchWeeklyScheduleQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        bool branchExists = await readService.BranchExistsAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure<BranchWeeklyScheduleResponse>(
                BranchScheduleErrors.BranchNotFound(query.BranchId));
        }

        BranchWeeklyScheduleResponse? response =
            await readService.GetWeeklyScheduleAsync(branchId, cancellationToken);

        return Result.Success(response!);
    }
}
