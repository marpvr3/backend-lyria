namespace Lyria.Application.Features.PublicCatalog;

/// <summary>
/// Detalle completo de una sede para vista pública.
/// </summary>
public sealed record PublicBranchDetailResponse(
    Guid Id,
    string Name,
    PublicBranchAddressResponse Address,
    PublicBranchLocationResponse? Location,
    PublicBranchContactResponse Contact,
    IReadOnlyList<PublicBranchServiceResponse> Services,
    IReadOnlyList<PublicBranchRestrictionResponse> Restrictions,
    IReadOnlyList<PublicBranchDayScheduleResponse> Schedules,
    IReadOnlyList<PublicBranchImageResponse> Images,
    PublicBranchAvailabilityResponse Availability);

/// <summary>
/// Dirección de una sede.
/// </summary>
public sealed record PublicBranchAddressResponse(
    string Street,
    string? Number,
    string? AddressComplement,
    string? Neighborhood,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country);

/// <summary>
/// Coordenadas geográficas de una sede.
/// </summary>
public sealed record PublicBranchLocationResponse(
    decimal Latitude,
    decimal Longitude);

/// <summary>
/// Información de contacto de una sede.
/// </summary>
public sealed record PublicBranchContactResponse(
    string? Phone,
    string? WhatsApp,
    string? Email);

/// <summary>
/// Servicio asociado a una sede pública.
/// </summary>
public sealed record PublicBranchServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    bool IsAvailable,
    string? Observation);

/// <summary>
/// Restricción asociada a una sede pública.
/// </summary>
public sealed record PublicBranchRestrictionResponse(
    Guid Id,
    string Name,
    string? Description,
    int ComplianceLevel,
    string ComplianceLevelName,
    bool IsCertified,
    string? Observation);

/// <summary>
/// Programación de un día para vista pública.
/// </summary>
public sealed record PublicBranchDayScheduleResponse(
    int DayOfWeek,
    string DayName,
    bool IsClosed,
    IReadOnlyList<PublicBranchTimeSlotResponse> TimeSlots);

/// <summary>
/// Franja horaria para vista pública.
/// </summary>
public sealed record PublicBranchTimeSlotResponse(
    string? OpeningTime,
    string? ClosingTime,
    bool CrossesMidnight);

/// <summary>
/// Imagen de una sede para vista pública.
/// </summary>
public sealed record PublicBranchImageResponse(
    Guid Id,
    string Url,
    string? AlternativeText,
    bool IsPrimary,
    int SortOrder);

/// <summary>
/// Detalle consolidado de una sede con información del establecimiento padre.
/// </summary>
public sealed record PublicBranchFullDetailResponse(
    PublicBranchEstablishmentResponse Establishment,
    PublicBranchDetailResponse Branch);

/// <summary>
/// Información básica del establecimiento padre para el detalle de sede.
/// </summary>
public sealed record PublicBranchEstablishmentResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    PublicCategoryDetailResponse Category);
