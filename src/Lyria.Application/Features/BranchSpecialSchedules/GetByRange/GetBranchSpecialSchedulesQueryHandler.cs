using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetByRange;

public sealed class GetBranchSpecialSchedulesQueryHandler(
    IBranchSpecialScheduleReadService readService)
    : IQueryHandler<GetBranchSpecialSchedulesQuery, Result<BranchSpecialSchedulesResponse>>
{
    public async ValueTask<Result<BranchSpecialSchedulesResponse>> Handle(
        GetBranchSpecialSchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        bool branchExists = await readService.BranchExistsAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure<BranchSpecialSchedulesResponse>(
                BranchSpecialScheduleErrors.BranchNotFound(query.BranchId));
        }

        BranchSpecialSchedulesResponse? response =
            await readService.GetByDateRangeAsync(branchId, query.From, query.To, cancellationToken);

        return Result.Success(response!);
    }
}
