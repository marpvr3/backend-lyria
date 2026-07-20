using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class EstablishmentBranchRestrictionRepository(LyriaDbContext dbContext)
    : IEstablishmentBranchRestrictionRepository
{
    public async Task<EstablishmentBranchRestriction?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchRestriction>()
            .FirstOrDefaultAsync(
                x => x.BranchId == branchId && x.RestrictionId == restrictionId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchRestriction>()
            .AnyAsync(
                x => x.BranchId == branchId && x.RestrictionId == restrictionId,
                cancellationToken);
    }

    public void Add(EstablishmentBranchRestriction branchRestriction)
    {
        dbContext.Set<EstablishmentBranchRestriction>().Add(branchRestriction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
