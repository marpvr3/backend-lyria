using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Roles;
using Lyria.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class RoleReadService(LyriaDbContext dbContext)
    : IRoleReadService
{
    public async Task<RoleResponse?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Role>()
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleResponse(
                r.Id.Value,
                r.Code,
                r.Name,
                r.Description,
                r.IsActive,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<RoleListItemResponse>> ListAsync(
        RoleListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Role>()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r =>
                r.Code.Contains(search) ||
                r.Name.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            string code = filter.Code.Trim();
            query = query.Where(r => r.Code == code);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        bool descending = string.Equals(
            filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Role> orderedQuery = filter.SortBy?.ToLowerInvariant() switch
        {
            "code" => descending
                ? query.OrderByDescending(r => r.Code)
                : query.OrderBy(r => r.Code),
            "createdatutc" => descending
                ? query.OrderByDescending(r => r.CreatedAtUtc)
                : query.OrderBy(r => r.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name)
        };

        var items = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new RoleListItemResponse(
                r.Id.Value,
                r.Code,
                r.Name,
                r.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<RoleListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
