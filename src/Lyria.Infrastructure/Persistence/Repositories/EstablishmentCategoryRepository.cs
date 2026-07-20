using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class EstablishmentCategoryRepository(LyriaDbContext dbContext)
    : IEstablishmentCategoryRepository
{
    public async Task<EstablishmentCategory?> GetByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentCategory>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string normalizedName,
        EstablishmentCategoryId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentCategory>()
            .AnyAsync(
                c => c.Name == normalizedName &&
                     (excludingId == null || c.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        EstablishmentCategory category,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<EstablishmentCategory>()
            .AddAsync(category, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
