using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchImages.GetByBranch;

public sealed class GetBranchImagesQueryHandler(
    IBranchImageReadService readService)
    : IQueryHandler<GetBranchImagesQuery, Result<BranchImagesResponse>>
{
    public async ValueTask<Result<BranchImagesResponse>> Handle(
        GetBranchImagesQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        bool branchExists = await readService.BranchExistsAsync(branchId, cancellationToken);

        if (!branchExists)
        {
            return Result.Failure<BranchImagesResponse>(
                BranchImageErrors.BranchNotFound(query.BranchId));
        }

        BranchImagesResponse? response =
            await readService.GetByBranchIdAsync(branchId, cancellationToken);

        return Result.Success(response!);
    }
}
