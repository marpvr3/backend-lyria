using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class RestrictionRepository(LyriaDbContext dbContext)
    : IRestrictionRepository
{
    public async Task<Restriction?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Restriction>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string normalizedName,
        RestrictionId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Restriction>()
            .AnyAsync(
                r => r.Name == normalizedName &&
                     (excludingId == null || r.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Restriction restriction,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<Restriction>()
            .AddAsync(restriction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
