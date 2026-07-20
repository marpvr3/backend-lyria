using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Services;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class ServiceReadService(LyriaDbContext dbContext)
    : IServiceReadService
{
    public async Task<ServiceResponse?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Service>()
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ServiceResponse(
                s.Id.Value,
                s.Name,
                s.Description,
                s.IconUrl,
                s.IsActive,
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<ServiceListItemResponse>> ListAsync(
        ServiceListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Service>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(s => s.Name.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == filter.IsActive.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new ServiceListItemResponse(
                s.Id.Value,
                s.Name,
                s.Description,
                s.IconUrl,
                s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ServiceListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
