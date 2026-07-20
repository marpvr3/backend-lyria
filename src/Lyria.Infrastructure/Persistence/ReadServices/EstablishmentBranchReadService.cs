using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class EstablishmentBranchReadService(LyriaDbContext dbContext)
    : IEstablishmentBranchReadService
{
    public async Task<EstablishmentBranchResponse?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EstablishmentBranchResponse(
                e.Id.Value,
                e.EstablishmentId.Value,
                e.Name,
                e.Street,
                e.Number,
                e.AddressComplement,
                e.Neighborhood,
                e.City,
                e.Province,
                e.PostalCode,
                e.Country,
                FullAddressBuilder.Build(
                    e.Street, e.Number, e.AddressComplement,
                    e.Neighborhood, e.City, e.Province,
                    e.PostalCode, e.Country),
                e.Latitude,
                e.Longitude,
                e.Phone,
                e.WhatsApp,
                e.Email,
                e.RatingAverage,
                e.TotalReviews,
                e.IsActive,
                e.CreatedAtUtc,
                e.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
        EstablishmentBranchListFilter filter,
        CancellationToken cancellationToken)
    {
        var establishmentId = new EstablishmentId(filter.EstablishmentId);

        var query = dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(e => e.EstablishmentId == establishmentId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(e => e.Name.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == filter.IsActive.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(e => new EstablishmentBranchListItemResponse(
                e.Id.Value,
                e.EstablishmentId.Value,
                e.Name,
                FullAddressBuilder.Build(
                    e.Street, e.Number, e.AddressComplement,
                    e.Neighborhood, e.City, e.Province,
                    e.PostalCode, e.Country),
                e.City,
                e.Province,
                e.Latitude,
                e.Longitude,
                e.RatingAverage,
                e.TotalReviews,
                e.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EstablishmentBranchListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
