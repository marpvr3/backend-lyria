using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.PublicCatalog.GetBranchById;

public sealed class GetPublicBranchByIdQueryHandler(
    IPublicBranchReadService readService)
    : IQueryHandler<GetPublicBranchByIdQuery, Result<PublicBranchFullDetailResponse>>
{
    public async ValueTask<Result<PublicBranchFullDetailResponse>> Handle(
        GetPublicBranchByIdQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        var result = await readService.GetByIdAsync(branchId, cancellationToken);

        return result is null
            ? Result.Failure<PublicBranchFullDetailResponse>(
                PublicCatalogErrors.BranchNotFound(query.BranchId))
            : Result.Success(result);
    }
}
