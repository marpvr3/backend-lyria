using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class BranchImageRepository(LyriaDbContext dbContext)
    : IBranchImageRepository
{
    public async Task<BranchImage?> GetByIdAsync(
        BranchImageId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<BranchImage>()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<BranchImage>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<BranchImage>()
            .Where(i => i.BranchId == branchId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Add(BranchImage image)
    {
        dbContext.Set<BranchImage>().Add(image);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
