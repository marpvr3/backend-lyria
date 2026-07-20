using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class EstablishmentBranchServiceRepository(LyriaDbContext dbContext)
    : IEstablishmentBranchServiceRepository
{
    public async Task<EstablishmentBranchService?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchService>()
            .FirstOrDefaultAsync(
                x => x.BranchId == branchId && x.ServiceId == serviceId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchService>()
            .AnyAsync(
                x => x.BranchId == branchId && x.ServiceId == serviceId,
                cancellationToken);
    }

    public void Add(EstablishmentBranchService branchService)
    {
        dbContext.Set<EstablishmentBranchService>().Add(branchService);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
