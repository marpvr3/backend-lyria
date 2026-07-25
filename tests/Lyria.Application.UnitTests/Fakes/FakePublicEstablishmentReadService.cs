using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.PublicCatalog;

namespace Lyria.Application.UnitTests.Fakes;

/// <summary>
/// Datos internos para alimentar el fake del catálogo público de establecimientos.
/// </summary>
internal sealed record FakePublicEstablishmentSeed(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? PrimaryImageUrl,
    PublicCategoryBriefResponse Category,
    IReadOnlyList<FakePublicBranchSeed> Branches,
    DateTime CreatedAtUtc);

internal sealed record FakePublicBranchSeed(
    Guid Id,
    string Name,
    string? City,
    string? Province,
    string? Country,
    IReadOnlyList<FakePublicBranchServiceSeed> Services,
    IReadOnlyList<FakePublicBranchRestrictionSeed> Restrictions);

internal sealed record FakePublicBranchServiceSeed(Guid Id, string Name);

internal sealed record FakePublicBranchRestrictionSeed(
    Guid Id,
    string Name,
    int ComplianceLevel,
    bool IsCertified);

internal sealed class FakePublicEstablishmentReadService : IPublicEstablishmentReadService
{
    private readonly List<FakePublicEstablishmentSeed> _seeds = [];
    private readonly Dictionary<string, PublicEstablishmentDetailResponse> _detailsBySlug = new();

    public void Seed(FakePublicEstablishmentSeed seed)
    {
        _seeds.Add(seed);
    }

    public void SeedDetail(string normalizedSlug, PublicEstablishmentDetailResponse detail)
    {
        _detailsBySlug[normalizedSlug] = detail;
    }

    public Task<PagedResponse<PublicEstablishmentListItemResponse>> ListAsync(
        PublicEstablishmentListFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<FakePublicEstablishmentSeed> query = _seeds;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            query = query.Where(s =>
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (s.Description is not null && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(s => s.Category.Id == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            string city = filter.City;
            query = query.Where(s => s.Branches.Any(b =>
                string.Equals(b.City, city, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            string province = filter.Province;
            query = query.Where(s => s.Branches.Any(b =>
                string.Equals(b.Province, province, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            string country = filter.Country;
            query = query.Where(s => s.Branches.Any(b =>
                string.Equals(b.Country, country, StringComparison.OrdinalIgnoreCase)));
        }

        // Service + restriction filters respect "same branch" semantics
        if (filter.ServiceId.HasValue || filter.RestrictionId.HasValue ||
            filter.ComplianceLevel.HasValue || filter.IsCertified.HasValue)
        {
            query = query.Where(s => s.Branches.Any(b =>
            {
                bool match = true;

                if (filter.ServiceId.HasValue)
                {
                    match = match && b.Services.Any(svc => svc.Id == filter.ServiceId.Value);
                }

                if (filter.RestrictionId.HasValue)
                {
                    match = match && b.Restrictions.Any(r => r.Id == filter.RestrictionId.Value);
                }

                if (filter.ComplianceLevel.HasValue)
                {
                    match = match && b.Restrictions.Any(r => r.ComplianceLevel == filter.ComplianceLevel.Value);
                }

                if (filter.IsCertified.HasValue)
                {
                    match = match && b.Restrictions.Any(r => r.IsCertified == filter.IsCertified.Value);
                }

                return match;
            }));
        }

        List<FakePublicEstablishmentSeed> sorted = (filter.SortBy, filter.SortDirection) switch
        {
            ("name", "desc") => query.OrderByDescending(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            ("newest", "asc") => query.OrderBy(s => s.CreatedAtUtc).ToList(),
            ("newest", "desc") => query.OrderByDescending(s => s.CreatedAtUtc).ToList(),
            ("newest", _) => query.OrderByDescending(s => s.CreatedAtUtc).ToList(),
            ("branchcount", "asc") => query.OrderBy(s => s.Branches.Count).ToList(),
            ("branchcount", "desc") => query.OrderByDescending(s => s.Branches.Count).ToList(),
            ("branchcount", _) => query.OrderByDescending(s => s.Branches.Count).ToList(),
            _ => query.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        int totalItems = sorted.Count;

        var items = sorted
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(ToListItem)
            .ToList();

        return Task.FromResult(new PagedResponse<PublicEstablishmentListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }

    public Task<PublicEstablishmentDetailResponse?> GetBySlugAsync(
        string normalizedSlug,
        CancellationToken cancellationToken)
    {
        _detailsBySlug.TryGetValue(normalizedSlug, out var detail);
        return Task.FromResult(detail);
    }

    private static PublicEstablishmentListItemResponse ToListItem(FakePublicEstablishmentSeed seed)
    {
        var cities = seed.Branches
            .Where(b => b.City is not null)
            .Select(b => b.City!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var services = seed.Branches
            .SelectMany(b => b.Services)
            .DistinctBy(s => s.Id)
            .Select(s => new PublicServiceBriefResponse(s.Id, s.Name, null))
            .ToList();

        var restrictions = seed.Branches
            .SelectMany(b => b.Restrictions)
            .DistinctBy(r => r.Id)
            .Select(r => new PublicRestrictionBriefResponse(r.Id, r.Name))
            .ToList();

        return new PublicEstablishmentListItemResponse(
            seed.Id,
            seed.Name,
            seed.Slug,
            seed.Description,
            seed.LogoUrl,
            seed.PrimaryImageUrl,
            seed.Category,
            seed.Branches.Count,
            cities,
            services,
            restrictions);
    }
}
