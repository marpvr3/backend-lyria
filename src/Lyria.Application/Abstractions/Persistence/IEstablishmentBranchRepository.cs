using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchRepository
{
    Task<EstablishmentBranch?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameWithinEstablishmentAsync(
        EstablishmentId establishmentId,
        string normalizedName,
        EstablishmentBranchId? excludingId,
        CancellationToken cancellationToken);

    void Add(EstablishmentBranch branch);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
