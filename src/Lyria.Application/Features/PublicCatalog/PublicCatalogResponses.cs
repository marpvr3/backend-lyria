namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Catálogos activos para filtros del frontend.
/// </summary>
public sealed record PublicCatalogsResponse(
    IReadOnlyList<PublicCatalogCategoryResponse> Categories,
    IReadOnlyList<PublicCatalogServiceResponse> Services,
    IReadOnlyList<PublicCatalogRestrictionResponse> Restrictions,
    PublicCatalogLocationsResponse Locations);

/// <summary>
/// Categoría para filtros.
/// </summary>
public sealed record PublicCatalogCategoryResponse(
    Guid Id,
    string Name,
    string? IconUrl,
    int SortOrder);

/// <summary>
/// Servicio para filtros.
/// </summary>
public sealed record PublicCatalogServiceResponse(
    Guid Id,
    string Name,
    string? IconUrl);

/// <summary>
/// Restricción para filtros.
/// </summary>
public sealed record PublicCatalogRestrictionResponse(
    Guid Id,
    string Name);

/// <summary>
/// Ubicaciones disponibles para filtros.
/// </summary>
public sealed record PublicCatalogLocationsResponse(
    IReadOnlyList<string> Countries,
    IReadOnlyList<PublicCatalogProvinceResponse> Provinces,
    IReadOnlyList<PublicCatalogCityResponse> Cities);

/// <summary>
/// Provincia agrupada por país.
/// </summary>
public sealed record PublicCatalogProvinceResponse(
    string Country,
    string Name);

/// <summary>
/// Ciudad agrupada por país y provincia.
/// </summary>
public sealed record PublicCatalogCityResponse(
    string Country,
    string Province,
    string Name);
