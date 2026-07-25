using System.Globalization;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.BranchSchedules;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Establishments.Categories;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class PublicBranchReadService(LyriaDbContext dbContext)
    : IPublicBranchReadService
{
    public async Task<PublicBranchFullDetailResponse?> GetByIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        // Obtener sede activa con establecimiento activo y categoría activa
        var branchData = await dbContext.Set<EstablishmentBranch>()
            .AsNoTracking()
            .Where(b => b.Id == branchId && b.IsActive)
            .Join(
                dbContext.Set<Establishment>().AsNoTracking().Where(e => e.IsActive),
                b => b.EstablishmentId,
                e => e.Id,
                (b, e) => new { Branch = b, Establishment = e })
            .Join(
                dbContext.Set<EstablishmentCategory>().AsNoTracking().Where(c => c.IsActive),
                x => x.Establishment.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    x.Branch,
                    x.Establishment,
                    Category = c
                })
            .Select(x => new
            {
                BranchId = x.Branch.Id,
                BranchName = x.Branch.Name,
                x.Branch.Street,
                x.Branch.Number,
                x.Branch.AddressComplement,
                x.Branch.Neighborhood,
                x.Branch.City,
                x.Branch.Province,
                x.Branch.PostalCode,
                x.Branch.Country,
                x.Branch.Latitude,
                x.Branch.Longitude,
                BranchPhone = x.Branch.Phone,
                BranchWhatsApp = x.Branch.WhatsApp,
                BranchEmail = x.Branch.Email,
                EstablishmentId = x.Establishment.Id,
                EstablishmentName = x.Establishment.Name,
                EstablishmentSlug = x.Establishment.Slug,
                EstablishmentDescription = x.Establishment.Description,
                EstablishmentLogoUrl = x.Establishment.LogoUrl,
                CategoryId = x.Category.Id,
                CategoryName = x.Category.Name,
                CategoryDescription = x.Category.Description,
                CategoryIconUrl = x.Category.IconUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (branchData is null)
        {
            return null;
        }

        // Servicios
        var services = await (
            from bs in dbContext.Set<EstablishmentBranchService>().AsNoTracking()
            join s in dbContext.Set<Service>().AsNoTracking()
                on bs.ServiceId equals s.Id
            where bs.BranchId == branchId && bs.IsActive && s.IsActive
            orderby s.Name
            select new PublicBranchServiceResponse(
                s.Id.Value, s.Name, s.Description, s.IconUrl, bs.IsAvailable, bs.Observation)
        ).ToListAsync(cancellationToken);

        // Restricciones
        var restrictions = await (
            from br in dbContext.Set<EstablishmentBranchRestriction>().AsNoTracking()
            join r in dbContext.Set<Restriction>().AsNoTracking()
                on br.RestrictionId equals r.Id
            where br.BranchId == branchId && br.IsActive && r.IsActive
            orderby r.Name
            select new
            {
                RestrictionId = r.Id.Value,
                r.Name,
                r.Description,
                br.ComplianceLevel,
                br.IsCertified,
                br.Observation
            }
        ).ToListAsync(cancellationToken);

        var restrictionResponses = restrictions
            .Select(r => new PublicBranchRestrictionResponse(
                r.RestrictionId, r.Name, r.Description,
                (int)r.ComplianceLevel,
                PublicEstablishmentReadService.GetComplianceLevelName(r.ComplianceLevel),
                r.IsCertified, r.Observation))
            .ToList();

        // Horarios
        var scheduleData = await dbContext.Set<BranchSchedule>()
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.OpeningTime)
            .Select(s => new
            {
                s.DayOfWeek,
                s.IsClosed,
                s.OpeningTime,
                s.ClosingTime,
                s.CrossesMidnight
            })
            .ToListAsync(cancellationToken);

        var schedules = scheduleData
            .GroupBy(s => s.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new PublicBranchDayScheduleResponse(
                (int)g.Key,
                WeekDayNames.GetSpanishName(g.Key),
                g.Any(s => s.IsClosed),
                g.Where(s => !s.IsClosed)
                    .Select(s => new PublicBranchTimeSlotResponse(
                        s.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                        s.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                        s.CrossesMidnight))
                    .ToList()))
            .ToList();

        // Imágenes
        var images = await dbContext.Set<BranchImage>()
            .AsNoTracking()
            .Where(i => i.BranchId == branchId && i.IsActive)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .Select(i => new PublicBranchImageResponse(
                i.Id.Value, i.Url, i.AlternativeText, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

        var branchDetail = new PublicBranchDetailResponse(
            branchData.BranchId.Value,
            branchData.BranchName,
            new PublicBranchAddressResponse(
                branchData.Street, branchData.Number, branchData.AddressComplement,
                branchData.Neighborhood, branchData.City, branchData.Province,
                branchData.PostalCode, branchData.Country),
            branchData.Latitude.HasValue && branchData.Longitude.HasValue
                ? new PublicBranchLocationResponse(branchData.Latitude.Value, branchData.Longitude.Value)
                : null,
            new PublicBranchContactResponse(
                branchData.BranchPhone, branchData.BranchWhatsApp, branchData.BranchEmail),
            services,
            restrictionResponses,
            schedules,
            images);

        var establishmentResponse = new PublicBranchEstablishmentResponse(
            branchData.EstablishmentId.Value,
            branchData.EstablishmentName,
            branchData.EstablishmentSlug,
            branchData.EstablishmentDescription,
            branchData.EstablishmentLogoUrl,
            new PublicCategoryDetailResponse(
                branchData.CategoryId.Value,
                branchData.CategoryName,
                branchData.CategoryDescription,
                branchData.CategoryIconUrl));

        return new PublicBranchFullDetailResponse(establishmentResponse, branchDetail);
    }
}
