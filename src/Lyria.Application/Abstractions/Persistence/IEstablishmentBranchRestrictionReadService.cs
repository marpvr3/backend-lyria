using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchRestrictionReadService
{
    Task<EstablishmentBranchRestrictionResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);

    Task<PagedResponse<EstablishmentBranchRestrictionListItemResponse>> ListByBranchAsync(
        EstablishmentBranchRestrictionListFilter filter,
        CancellationToken cancellationToken);
}
