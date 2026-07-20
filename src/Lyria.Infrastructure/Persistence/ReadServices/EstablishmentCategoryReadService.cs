using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class EstablishmentCategoryReadService(LyriaDbContext dbContext)
    : IEstablishmentCategoryReadService
{
    public async Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentCategory>()
            .AsNoTracking()
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new EstablishmentCategoryResponse(
                c.Id.Value,
                c.Name,
                c.Description,
                c.IconUrl,
                c.SortOrder,
                c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentCategory>()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new EstablishmentCategoryResponse(
                c.Id.Value,
                c.Name,
                c.Description,
                c.IconUrl,
                c.SortOrder,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
