namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Resumen compacto de un establecimiento para tarjetas del catálogo público.
/// </summary>
public sealed record PublicEstablishmentListItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? PrimaryImageUrl,
    PublicCategoryBriefResponse Category,
    int BranchCount,
    IReadOnlyList<string> Cities,
    IReadOnlyList<PublicServiceBriefResponse> Services,
    IReadOnlyList<PublicRestrictionBriefResponse> Restrictions);

/// <summary>
/// Detalle completo de un establecimiento para vista pública.
/// </summary>
public sealed record PublicEstablishmentDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone,
    PublicCategoryDetailResponse Category,
    IReadOnlyList<PublicBranchDetailResponse> Branches);

/// <summary>
/// Categoría resumida para listados.
/// </summary>
public sealed record PublicCategoryBriefResponse(
    Guid Id,
    string Name);

/// <summary>
/// Categoría detallada para vista de establecimiento.
/// </summary>
public sealed record PublicCategoryDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl);

/// <summary>
/// Servicio resumido para listados.
/// </summary>
public sealed record PublicServiceBriefResponse(
    Guid Id,
    string Name,
    string? IconUrl);

/// <summary>
/// Restricción resumida para listados.
/// </summary>
public sealed record PublicRestrictionBriefResponse(
    Guid Id,
    string Name);
