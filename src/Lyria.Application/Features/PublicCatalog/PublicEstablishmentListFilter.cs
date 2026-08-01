namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Filtros para el catálogo público de establecimientos.
/// </summary>
public sealed record PublicEstablishmentListFilter(
    string? Search,
    Guid? CategoryId,
    string? City,
    string? Province,
    string? Country,
    Guid? ServiceId,
    Guid? RestrictionId,
    int? ComplianceLevel,
    bool? IsCertified,
    bool? OpenNow,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection,
    DateTimeOffset? EvaluatedAtUtc = null);
