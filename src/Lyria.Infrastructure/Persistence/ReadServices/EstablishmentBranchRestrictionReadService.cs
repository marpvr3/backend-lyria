using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class EstablishmentBranchRestrictionReadService(LyriaDbContext dbContext)
    : IEstablishmentBranchRestrictionReadService
{
    public async Task<EstablishmentBranchRestrictionResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranchRestriction>()
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && x.RestrictionId == restrictionId)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking(),
                br => br.RestrictionId,
                r => r.Id,
                (br, r) => new EstablishmentBranchRestrictionResponse(
                    br.BranchId.Value,
                    br.RestrictionId.Value,
                    r.Name,
                    r.Description,
                    br.ComplianceLevel,
                    br.IsCertified,
                    br.Observation,
                    br.IsActive,
                    br.CreatedAtUtc,
                    br.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<EstablishmentBranchRestrictionListItemResponse>> ListByBranchAsync(
        EstablishmentBranchRestrictionListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<EstablishmentBranchRestriction>()
            .AsNoTracking()
            .Where(x => x.BranchId == filter.BranchId)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking(),
                br => br.RestrictionId,
                r => r.Id,
                (br, r) => new { BranchRestriction = br, Restriction = r });

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(x => x.Restriction.Name.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.BranchRestriction.IsActive == filter.IsActive.Value);
        }

        if (filter.ComplianceLevel.HasValue)
        {
            query = query.Where(x => x.BranchRestriction.ComplianceLevel == filter.ComplianceLevel.Value);
        }

        if (filter.IsCertified.HasValue)
        {
            query = query.Where(x => x.BranchRestriction.IsCertified == filter.IsCertified.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Restriction.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EstablishmentBranchRestrictionListItemResponse(
                x.BranchRestriction.BranchId.Value,
                x.BranchRestriction.RestrictionId.Value,
                x.Restriction.Name,
                x.BranchRestriction.ComplianceLevel,
                x.BranchRestriction.IsCertified,
                x.BranchRestriction.Observation,
                x.BranchRestriction.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EstablishmentBranchRestrictionListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
