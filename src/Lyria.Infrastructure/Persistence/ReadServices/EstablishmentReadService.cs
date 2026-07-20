using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Establishments;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class EstablishmentReadService(LyriaDbContext dbContext)
    : IEstablishmentReadService
{
    public async Task<EstablishmentResponse?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Establishment>()
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Join(
                dbContext.Set<EstablishmentCategory>().AsNoTracking(),
                e => e.CategoryId,
                c => c.Id,
                (e, c) => new EstablishmentResponse(
                    e.Id.Value,
                    e.CategoryId.Value,
                    c.Name,
                    e.Name,
                    e.Slug,
                    e.Description,
                    e.Website,
                    e.Instagram,
                    e.LogoUrl,
                    e.ContactEmail,
                    e.ContactPhone,
                    e.IsVerified,
                    e.VerifiedAtUtc,
                    e.IsActive,
                    e.CreatedAtUtc,
                    e.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
        EstablishmentListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Establishment>()
            .AsNoTracking()
            .Join(
                dbContext.Set<EstablishmentCategory>().AsNoTracking(),
                e => e.CategoryId,
                c => c.Id,
                (e, c) => new { Establishment = e, CategoryName = c.Name });

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(x =>
                x.Establishment.Name.Contains(search) ||
                x.Establishment.Slug.Contains(search));
        }

        if (filter.CategoryId.HasValue)
        {
            var categoryId = new EstablishmentCategoryId(filter.CategoryId.Value);
            query = query.Where(x => x.Establishment.CategoryId == categoryId);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.Establishment.IsActive == filter.IsActive.Value);
        }

        if (filter.IsVerified.HasValue)
        {
            query = query.Where(x => x.Establishment.IsVerified == filter.IsVerified.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Establishment.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EstablishmentListItemResponse(
                x.Establishment.Id.Value,
                x.Establishment.CategoryId.Value,
                x.CategoryName,
                x.Establishment.Name,
                x.Establishment.Slug,
                x.Establishment.IsVerified,
                x.Establishment.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EstablishmentListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
