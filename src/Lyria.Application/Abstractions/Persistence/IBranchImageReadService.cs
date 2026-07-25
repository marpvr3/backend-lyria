using Lyria.Application.Features.BranchImages;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IBranchImageReadService
{
    Task<bool> BranchExistsAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);

    Task<BranchImagesResponse?> GetByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);
}
