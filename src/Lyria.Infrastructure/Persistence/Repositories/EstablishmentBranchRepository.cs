using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class EstablishmentBranchRepository(LyriaDbContext dbContext)
    : IEstablishmentBranchRepository
{
    public async Task<EstablishmentBranch?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameWithinEstablishmentAsync(
        EstablishmentId establishmentId,
        string normalizedName,
        EstablishmentBranchId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AnyAsync(
                e => e.EstablishmentId == establishmentId &&
                     e.Name == normalizedName &&
                     (excludingId == null || e.Id != excludingId.Value),
                cancellationToken);
    }

    public void Add(EstablishmentBranch branch)
    {
        dbContext.Set<EstablishmentBranch>().Add(branch);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
