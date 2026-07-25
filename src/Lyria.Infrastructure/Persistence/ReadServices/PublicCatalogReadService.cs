using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class PublicCatalogReadService(LyriaDbContext dbContext)
    : IPublicCatalogReadService
{
    public async Task<PublicCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken)
    {
        // Categorías activas
        var categories = await dbContext.Set<EstablishmentCategory>()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new PublicCatalogCategoryResponse(
                c.Id.Value, c.Name, c.IconUrl, c.SortOrder))
            .ToListAsync(cancellationToken);

        // Servicios activos
        var services = await dbContext.Set<Service>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new PublicCatalogServiceResponse(
                s.Id.Value, s.Name, s.IconUrl))
            .ToListAsync(cancellationToken);

        // Restricciones activas
        var restrictions = await dbContext.Set<Restriction>()
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new PublicCatalogRestrictionResponse(
                r.Id.Value, r.Name))
            .ToListAsync(cancellationToken);

        // Ubicaciones: sedes activas de establecimientos activos con categoría activa
        var activeEstablishmentIds = dbContext.Set<Establishment>()
            .Where(e => e.IsActive)
            .Join(
                dbContext.Set<EstablishmentCategory>().Where(c => c.IsActive),
                e => e.CategoryId,
                c => c.Id,
                (e, _) => e.Id);

        var locations = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => b.IsActive && activeEstablishmentIds.Contains(b.EstablishmentId))
            .Where(b => b.Country != null || b.Province != null || b.City != null)
            .Select(b => new
            {
                b.Country,
                b.Province,
                b.City
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var countries = locations
            .Where(l => l.Country is not null)
            .Select(l => l.Country!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var provinces = locations
            .Where(l => l.Country is not null && l.Province is not null)
            .Select(l => new PublicCatalogProvinceResponse(l.Country!, l.Province!))
            .Distinct()
            .OrderBy(p => p.Country)
            .ThenBy(p => p.Name)
            .ToList();

        var cities = locations
            .Where(l => l.Country is not null && l.Province is not null && l.City is not null)
            .Select(l => new PublicCatalogCityResponse(l.Country!, l.Province!, l.City!))
            .Distinct()
            .OrderBy(c => c.Country)
            .ThenBy(c => c.Province)
            .ThenBy(c => c.Name)
            .ToList();

        return new PublicCatalogsResponse(
            categories,
            services,
            restrictions,
            new PublicCatalogLocationsResponse(countries, provinces, cities));
    }
}
