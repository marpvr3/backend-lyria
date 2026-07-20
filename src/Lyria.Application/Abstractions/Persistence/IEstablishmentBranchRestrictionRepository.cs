using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchRestrictionRepository
{
    Task<EstablishmentBranchRestriction?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken);

    void Add(EstablishmentBranchRestriction branchRestriction);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
