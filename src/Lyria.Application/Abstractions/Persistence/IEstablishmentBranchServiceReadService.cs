using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchServiceReadService
{
    Task<EstablishmentBranchServiceResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken);

    Task<PagedResponse<EstablishmentBranchServiceListItemResponse>> ListByBranchAsync(
        EstablishmentBranchServiceListFilter filter,
        CancellationToken cancellationToken);
}
