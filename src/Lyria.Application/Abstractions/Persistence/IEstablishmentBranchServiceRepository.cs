using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentBranchServiceRepository
{
    Task<EstablishmentBranchService?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken);

    void Add(EstablishmentBranchService branchService);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
