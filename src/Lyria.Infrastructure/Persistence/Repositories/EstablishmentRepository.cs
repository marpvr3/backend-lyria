using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class EstablishmentRepository(LyriaDbContext dbContext)
    : IEstablishmentRepository
{
    public async Task<Establishment?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Establishment>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(
        string normalizedSlug,
        EstablishmentId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Establishment>()
            .AnyAsync(
                e => e.Slug == normalizedSlug &&
                     (excludingId == null || e.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Establishment establishment,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<Establishment>()
            .AddAsync(establishment, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
