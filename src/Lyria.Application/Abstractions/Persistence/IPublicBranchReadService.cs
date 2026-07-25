using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IPublicBranchReadService
{
    Task<PublicBranchFullDetailResponse?> GetByIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken);
}
