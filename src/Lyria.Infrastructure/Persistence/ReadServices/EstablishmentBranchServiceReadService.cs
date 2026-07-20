using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class EstablishmentBranchServiceReadService(LyriaDbContext dbContext)
    : IEstablishmentBranchServiceReadService
{
    public async Task<EstablishmentBranchServiceResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchService>()
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && x.ServiceId == serviceId)
            .Join(
                dbContext.Set<Service>().AsNoTracking(),
                bs => bs.ServiceId,
                s => s.Id,
                (bs, s) => new EstablishmentBranchServiceResponse(
                    bs.BranchId.Value,
                    bs.ServiceId.Value,
                    s.Name,
                    s.Description,
                    s.IconUrl,
                    bs.IsAvailable,
                    bs.Observation,
                    bs.IsActive,
                    bs.CreatedAtUtc,
                    bs.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<EstablishmentBranchServiceListItemResponse>> ListByBranchAsync(
        EstablishmentBranchServiceListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<EstablishmentBranchService>()
            .AsNoTracking()
            .Where(x => x.BranchId == filter.BranchId)
            .Join(
                dbContext.Set<Service>().AsNoTracking(),
                bs => bs.ServiceId,
                s => s.Id,
                (bs, s) => new { BranchService = bs, Service = s });

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(x => x.Service.Name.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.BranchService.IsActive == filter.IsActive.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(x => x.BranchService.IsAvailable == filter.IsAvailable.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Service.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EstablishmentBranchServiceListItemResponse(
                x.BranchService.BranchId.Value,
                x.BranchService.ServiceId.Value,
                x.Service.Name,
                x.Service.IconUrl,
                x.BranchService.IsAvailable,
                x.BranchService.Observation,
                x.BranchService.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EstablishmentBranchServiceListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
