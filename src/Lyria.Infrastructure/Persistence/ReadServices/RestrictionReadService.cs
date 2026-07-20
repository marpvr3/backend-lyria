using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class RestrictionReadService(LyriaDbContext dbContext)
    : IRestrictionReadService
{
    public async Task<RestrictionResponse?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Restriction>()
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RestrictionResponse(
                r.Id.Value,
                r.Name,
                r.Description,
                r.IsActive,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<RestrictionListItemResponse>> ListAsync(
        RestrictionListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Restriction>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r => r.Name.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new RestrictionListItemResponse(
                r.Id.Value,
                r.Name,
                r.Description,
                r.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<RestrictionListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
