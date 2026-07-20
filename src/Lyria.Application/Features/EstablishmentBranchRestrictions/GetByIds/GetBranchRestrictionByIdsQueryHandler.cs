using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;

public sealed class GetBranchRestrictionByIdsQueryHandler(
    IEstablishmentBranchRestrictionReadService readService)
    : IQueryHandler<GetBranchRestrictionByIdsQuery, Result<EstablishmentBranchRestrictionResponse>>
{
    public async ValueTask<Result<EstablishmentBranchRestrictionResponse>> Handle(
        GetBranchRestrictionByIdsQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);
        var restrictionId = new RestrictionId(query.RestrictionId);

        EstablishmentBranchRestrictionResponse? response =
            await readService.GetByIdsAsync(branchId, restrictionId, cancellationToken);

        if (response is null)
        {
            return Result.Failure<EstablishmentBranchRestrictionResponse>(
                EstablishmentBranchRestrictionErrors.NotFound(query.BranchId, query.RestrictionId));
        }

        return Result.Success(response);
    }
}
