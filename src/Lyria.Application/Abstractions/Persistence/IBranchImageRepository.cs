using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchImageRepository
{
    Task<BranchImage?> GetByIdAsync(
        BranchImageId id,
        CancellationToken cancellationToken);

    Task<List<BranchImage>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    void Add(BranchImage image);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
