using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchReadService
{
    Task<EstablishmentBranchResponse?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
        EstablishmentBranchListFilter filter,
        CancellationToken cancellationToken);
}
