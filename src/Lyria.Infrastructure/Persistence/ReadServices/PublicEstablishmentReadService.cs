using System.Globalization;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class PublicEstablishmentReadService(LyriaDbContext dbContext)
    : IPublicEstablishmentReadService
{
    public async Task<PagedResponse<PublicEstablishmentListItemResponse>> ListAsync(
        PublicEstablishmentListFilter filter,
        CancellationToken cancellationToken)
    {
        // Base: establecimientos activos con categoría activa y al menos una sede activa
        var baseQuery = dbContext.Set<Establishment>()
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Join(
                dbContext.Set<EstablishmentCategory>().AsNoTracking().Where(c => c.IsActive),
                e => e.CategoryId,
                c => c.Id,
                (e, c) => new { Establishment = e, Category = c })
            .Where(x => dbContext.Set<EstablishmentBranch>()
                .Any(b => b.EstablishmentId == x.Establishment.Id && b.IsActive));

        // Filtro por búsqueda
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.Establishment.Name, $"%{search}%") ||
                (x.Establishment.Description != null &&
                 EF.Functions.Like(x.Establishment.Description, $"%{search}%")));
        }

        // Filtro por categoría
        if (filter.CategoryId.HasValue)
        {
            var categoryId = new EstablishmentCategoryId(filter.CategoryId.Value);
            baseQuery = baseQuery.Where(x => x.Establishment.CategoryId == categoryId);
        }

        // Filtro por ubicación
        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            string city = filter.City;
            baseQuery = baseQuery.Where(x => dbContext.Set<EstablishmentBranch>()
                .Any(b => b.EstablishmentId == x.Establishment.Id
                          && b.IsActive
                          && b.City != null
                          && EF.Functions.Like(b.City, city)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            string province = filter.Province;
            baseQuery = baseQuery.Where(x => dbContext.Set<EstablishmentBranch>()
                .Any(b => b.EstablishmentId == x.Establishment.Id
                          && b.IsActive
                          && b.Province != null
                          && EF.Functions.Like(b.Province, province)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            string country = filter.Country;
            baseQuery = baseQuery.Where(x => dbContext.Set<EstablishmentBranch>()
                .Any(b => b.EstablishmentId == x.Establishment.Id
                          && b.IsActive
                          && b.Country != null
                          && EF.Functions.Like(b.Country, country)));
        }

        // Filtros de servicio, restricción, compliance, certificación en la misma sede (AND)
        bool hasServiceFilter = filter.ServiceId.HasValue;
        bool hasRestrictionFilter = filter.RestrictionId.HasValue;
        bool hasComplianceFilter = filter.ComplianceLevel.HasValue;
        bool hasCertifiedFilter = filter.IsCertified.HasValue;

        if (hasServiceFilter || hasRestrictionFilter || hasComplianceFilter || hasCertifiedFilter)
        {
            baseQuery = baseQuery.Where(x => dbContext.Set<EstablishmentBranch>()
                .Where(b => b.EstablishmentId == x.Establishment.Id && b.IsActive)
                .Any(b =>
                    (!hasServiceFilter ||
                     dbContext.Set<EstablishmentBranchService>()
                         .Any(bs => bs.BranchId == b.Id
                                    && bs.ServiceId == new ServiceId(filter.ServiceId!.Value)
                                    && bs.IsActive
                                    && bs.IsAvailable))
                    &&
                    (!hasRestrictionFilter ||
                     dbContext.Set<EstablishmentBranchRestriction>()
                         .Any(br => br.BranchId == b.Id
                                    && br.RestrictionId == new RestrictionId(filter.RestrictionId!.Value)
                                    && br.IsActive))
                    &&
                    (!hasComplianceFilter ||
                     dbContext.Set<EstablishmentBranchRestriction>()
                         .Any(br => br.BranchId == b.Id
                                    && br.IsActive
                                    && (int)br.ComplianceLevel == filter.ComplianceLevel!.Value))
                    &&
                    (!hasCertifiedFilter ||
                     dbContext.Set<EstablishmentBranchRestriction>()
                         .Any(br => br.BranchId == b.Id
                                    && br.IsActive
                                    && br.IsCertified == filter.IsCertified!.Value))
                ));
        }

        // Contar total antes de paginar
        int totalItems = await baseQuery.CountAsync(cancellationToken);

        // Aplicar ordenamiento
        var orderedQuery = filter.SortBy switch
        {
            "newest" => filter.SortDirection == "desc"
                ? baseQuery.OrderByDescending(x => x.Establishment.CreatedAtUtc)
                    .ThenBy(x => x.Establishment.Id)
                : baseQuery.OrderBy(x => x.Establishment.CreatedAtUtc)
                    .ThenBy(x => x.Establishment.Id),
            "branchcount" => filter.SortDirection == "desc"
                ? baseQuery.OrderByDescending(x => dbContext.Set<EstablishmentBranch>()
                        .Count(b => b.EstablishmentId == x.Establishment.Id && b.IsActive))
                    .ThenBy(x => x.Establishment.Id)
                : baseQuery.OrderBy(x => dbContext.Set<EstablishmentBranch>()
                        .Count(b => b.EstablishmentId == x.Establishment.Id && b.IsActive))
                    .ThenBy(x => x.Establishment.Id),
            _ => filter.SortDirection == "desc"
                ? baseQuery.OrderByDescending(x => x.Establishment.Name)
                    .ThenBy(x => x.Establishment.Id)
                : baseQuery.OrderBy(x => x.Establishment.Name)
                    .ThenBy(x => x.Establishment.Id)
        };

        // Paginar y obtener IDs
        var pagedEstablishments = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new
            {
                EstablishmentId = x.Establishment.Id,
                x.Establishment.Name,
                x.Establishment.Slug,
                x.Establishment.Description,
                x.Establishment.LogoUrl,
                CategoryId = x.Category.Id,
                CategoryName = x.Category.Name
            })
            .ToListAsync(cancellationToken);

        if (pagedEstablishments.Count == 0)
        {
            return new PagedResponse<PublicEstablishmentListItemResponse>(
                [], filter.Page, filter.PageSize, totalItems);
        }

        var establishmentIds = pagedEstablishments
            .Select(e => e.EstablishmentId)
            .ToList();

        // Consultar sedes activas para obtener branchCount, cities y primaryImageUrl
        var branchData = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => establishmentIds.Contains(b.EstablishmentId) && b.IsActive)
            .Select(b => new
            {
                b.EstablishmentId,
                b.Id,
                b.City
            })
            .ToListAsync(cancellationToken);

        var branchCountByEstablishment = branchData
            .GroupBy(b => b.EstablishmentId)
            .ToDictionary(g => g.Key, g => g.Count());

        var citiesByEstablishment = branchData
            .Where(b => b.City is not null)
            .GroupBy(b => b.EstablishmentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(b => b.City!).Distinct().OrderBy(c => c).ToList());

        // Obtener IDs de sedes para consultar imágenes principales
        var branchIds = branchData.Select(b => b.Id).ToList();

        var primaryImages = await dbContext.Set<BranchImage>()
            .AsNoTracking()
            .Where(i => branchIds.Contains(i.BranchId) && i.IsActive && i.IsPrimary)
            .Select(i => new { i.BranchId, i.Url })
            .ToListAsync(cancellationToken);

        var primaryImageByEstablishment = primaryImages
            .Join(branchData,
                i => i.BranchId,
                b => b.Id,
                (i, b) => new { b.EstablishmentId, i.Url })
            .GroupBy(x => x.EstablishmentId)
            .ToDictionary(g => g.Key, g => g.First().Url);

        // Consultar servicios activos por establecimiento (a través de sedes activas)
        var servicesByEstablishment = await dbContext.Set<EstablishmentBranchService>()
            .AsNoTracking()
            .Where(bs => branchIds.Contains(bs.BranchId)
                         && bs.IsActive
                         && bs.IsAvailable)
            .Join(
                dbContext.Set<Service>().AsNoTracking().Where(s => s.IsActive),
                bs => bs.ServiceId,
                s => s.Id,
                (bs, s) => new { bs.BranchId, ServiceId = s.Id, s.Name, s.IconUrl })
            .ToListAsync(cancellationToken);

        var servicesDictionary = servicesByEstablishment
            .Join(branchData,
                s => s.BranchId,
                b => b.Id,
                (s, b) => new { b.EstablishmentId, s.ServiceId, s.Name, s.IconUrl })
            .GroupBy(x => x.EstablishmentId)
            .ToDictionary(
                g => g.Key,
                g => g.DistinctBy(s => s.ServiceId)
                    .OrderBy(s => s.Name)
                    .Select(s => new PublicServiceBriefResponse(s.ServiceId.Value, s.Name, s.IconUrl))
                    .ToList() as IReadOnlyList<PublicServiceBriefResponse>);

        // Consultar restricciones activas por establecimiento
        var restrictionsByEstablishment = await dbContext.Set<EstablishmentBranchRestriction>()
            .AsNoTracking()
            .Where(br => branchIds.Contains(br.BranchId) && br.IsActive)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking().Where(r => r.IsActive),
                br => br.RestrictionId,
                r => r.Id,
                (br, r) => new { br.BranchId, RestrictionId = r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var restrictionsDictionary = restrictionsByEstablishment
            .Join(branchData,
                r => r.BranchId,
                b => b.Id,
                (r, b) => new { b.EstablishmentId, r.RestrictionId, r.Name })
            .GroupBy(x => x.EstablishmentId)
            .ToDictionary(
                g => g.Key,
                g => g.DistinctBy(r => r.RestrictionId)
                    .OrderBy(r => r.Name)
                    .Select(r => new PublicRestrictionBriefResponse(r.RestrictionId.Value, r.Name))
                    .ToList() as IReadOnlyList<PublicRestrictionBriefResponse>);

        // Ensamblar respuesta
        var items = pagedEstablishments.Select(e =>
            new PublicEstablishmentListItemResponse(
                e.EstablishmentId.Value,
                e.Name,
                e.Slug,
                e.Description,
                e.LogoUrl,
                primaryImageByEstablishment.GetValueOrDefault(e.EstablishmentId),
                new PublicCategoryBriefResponse(e.CategoryId.Value, e.CategoryName),
                branchCountByEstablishment.GetValueOrDefault(e.EstablishmentId, 0),
                citiesByEstablishment.GetValueOrDefault(e.EstablishmentId, []),
                servicesDictionary.GetValueOrDefault(e.EstablishmentId, []),
                restrictionsDictionary.GetValueOrDefault(e.EstablishmentId, [])))
            .ToList();

        return new PagedResponse<PublicEstablishmentListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }

    public async Task<PublicEstablishmentDetailResponse?> GetBySlugAsync(
        string normalizedSlug,
        CancellationToken cancellationToken)
    {
        // Obtener establecimiento activo con categoría activa
        var establishment = await dbContext.Set<Establishment>()
            .AsNoTracking()
            .Where(e => e.Slug == normalizedSlug && e.IsActive)
            .Join(
                dbContext.Set<EstablishmentCategory>().AsNoTracking().Where(c => c.IsActive),
                e => e.CategoryId,
                c => c.Id,
                (e, c) => new
                {
                    e.Id,
                    e.Name,
                    e.Slug,
                    e.Description,
                    e.Website,
                    e.Instagram,
                    e.LogoUrl,
                    e.ContactEmail,
                    e.ContactPhone,
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    CategoryDescription = c.Description,
                    CategoryIconUrl = c.IconUrl
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (establishment is null)
        {
            return null;
        }

        // Sedes activas
        var branches = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => b.EstablishmentId == establishment.Id && b.IsActive)
            .OrderBy(b => b.Name)
            .ThenBy(b => b.Id)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Street,
                b.Number,
                b.AddressComplement,
                b.Neighborhood,
                b.City,
                b.Province,
                b.PostalCode,
                b.Country,
                b.Latitude,
                b.Longitude,
                b.Phone,
                b.WhatsApp,
                b.Email
            })
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            return null;
        }

        var branchIds = branches.Select(b => b.Id).ToList();

        // Servicios por sede
        var branchServices = await dbContext.Set<EstablishmentBranchService>()
            .AsNoTracking()
            .Where(bs => branchIds.Contains(bs.BranchId) && bs.IsActive)
            .Join(
                dbContext.Set<Service>().AsNoTracking().Where(s => s.IsActive),
                bs => bs.ServiceId,
                s => s.Id,
                (bs, s) => new
                {
                    bs.BranchId,
                    ServiceId = s.Id.Value,
                    s.Name,
                    s.Description,
                    s.IconUrl,
                    bs.IsAvailable,
                    bs.Observation
                })
            .ToListAsync(cancellationToken);

        var servicesByBranch = branchServices
            .GroupBy(s => s.BranchId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(s => s.Name)
                    .Select(s => new PublicBranchServiceResponse(
                        s.ServiceId, s.Name, s.Description, s.IconUrl, s.IsAvailable, s.Observation))
                    .ToList() as IReadOnlyList<PublicBranchServiceResponse>);

        // Restricciones por sede
        var branchRestrictions = await dbContext.Set<EstablishmentBranchRestriction>()
            .AsNoTracking()
            .Where(br => branchIds.Contains(br.BranchId) && br.IsActive)
            .Join(
                dbContext.Set<Restriction>().AsNoTracking().Where(r => r.IsActive),
                br => br.RestrictionId,
                r => r.Id,
                (br, r) => new
                {
                    br.BranchId,
                    RestrictionId = r.Id.Value,
                    r.Name,
                    r.Description,
                    br.ComplianceLevel,
                    br.IsCertified,
                    br.Observation
                })
            .ToListAsync(cancellationToken);

        var restrictionsByBranch = branchRestrictions
            .GroupBy(r => r.BranchId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.Name)
                    .Select(r => new PublicBranchRestrictionResponse(
                        r.RestrictionId, r.Name, r.Description,
                        (int)r.ComplianceLevel,
                        GetComplianceLevelName(r.ComplianceLevel),
                        r.IsCertified, r.Observation))
                    .ToList() as IReadOnlyList<PublicBranchRestrictionResponse>);

        // Horarios por sede
        var branchSchedules = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => branchIds.Contains(s.BranchId) && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .Select(s => new
            {
                s.BranchId,
                s.DayOfWeek,
                s.IsClosed,
                s.OpeningTime,
                s.ClosingTime,
                s.CrossesMidnight
            })
            .ToListAsync(cancellationToken);

        var schedulesByBranch = branchSchedules
            .GroupBy(s => s.BranchId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(s => s.DayOfWeek)
                    .OrderBy(dg => dg.Key)
                    .Select(dg => new PublicBranchDayScheduleResponse(
                        (int)dg.Key,
                        WeekDayNames.GetSpanishName(dg.Key),
                        dg.Any(s => s.IsClosed),
                        dg.Where(s => !s.IsClosed)
                            .Select(s => new PublicBranchTimeSlotResponse(
                                s.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                                s.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                                s.CrossesMidnight))
                            .ToList()))
                    .ToList() as IReadOnlyList<PublicBranchDayScheduleResponse>);

        // Imágenes por sede
        var branchImages = await dbContext.Set<BranchImage>()
            .AsNoTracking()
            .Where(i => branchIds.Contains(i.BranchId) && i.IsActive)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .Select(i => new
            {
                i.BranchId,
                ImageId = i.Id.Value,
                i.Url,
                i.AlternativeText,
                i.IsPrimary,
                i.SortOrder
            })
            .ToListAsync(cancellationToken);

        var imagesByBranch = branchImages
            .GroupBy(i => i.BranchId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(i => new PublicBranchImageResponse(
                        i.ImageId, i.Url, i.AlternativeText, i.IsPrimary, i.SortOrder))
                    .ToList() as IReadOnlyList<PublicBranchImageResponse>);

        // Ensamblar sedes
        var branchResponses = branches.Select(b =>
            new PublicBranchDetailResponse(
                b.Id.Value,
                b.Name,
                new PublicBranchAddressResponse(
                    b.Street, b.Number, b.AddressComplement,
                    b.Neighborhood, b.City, b.Province, b.PostalCode, b.Country),
                b.Latitude.HasValue && b.Longitude.HasValue
                    ? new PublicBranchLocationResponse(b.Latitude.Value, b.Longitude.Value)
                    : null,
                new PublicBranchContactResponse(b.Phone, b.WhatsApp, b.Email),
                servicesByBranch.GetValueOrDefault(b.Id, []),
                restrictionsByBranch.GetValueOrDefault(b.Id, []),
                schedulesByBranch.GetValueOrDefault(b.Id, []),
                imagesByBranch.GetValueOrDefault(b.Id, [])))
            .ToList();

        return new PublicEstablishmentDetailResponse(
            establishment.Id.Value,
            establishment.Name,
            establishment.Slug,
            establishment.Description,
            establishment.Website,
            establishment.Instagram,
            establishment.LogoUrl,
            establishment.ContactEmail,
            establishment.ContactPhone,
            new PublicCategoryDetailResponse(
                establishment.CategoryId.Value,
                establishment.CategoryName,
                establishment.CategoryDescription,
                establishment.CategoryIconUrl),
            branchResponses);
    }

    internal static string GetComplianceLevelName(RestrictionComplianceLevel level) => level switch
    {
        RestrictionComplianceLevel.Guaranteed => "Garantizado",
        RestrictionComplianceLevel.Partial => "Parcial",
        RestrictionComplianceLevel.OnRequest => "Bajo solicitud",
        _ => level.ToString()
    };
}
