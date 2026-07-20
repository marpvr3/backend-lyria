namespace Lyria.Application.Features.EstablishmentBranches;

/// <summary>
/// Resumen de una sede para listados paginados.
/// </summary>
/// <param name="Id">Identificador único de la sede.</param>
/// <param name="EstablishmentId">Identificador del establecimiento al que pertenece la sede.</param>
/// <param name="Name">Nombre de la sede.</param>
/// <param name="FullAddress">Dirección completa formateada.</param>
/// <param name="City">Ciudad de la sede.</param>
/// <param name="Province">Provincia de la sede.</param>
/// <param name="Latitude">Latitud geográfica de la sede.</param>
/// <param name="Longitude">Longitud geográfica de la sede.</param>
/// <param name="RatingAverage">Calificación promedio de la sede.</param>
/// <param name="TotalReviews">Cantidad total de reseñas de la sede.</param>
/// <param name="IsActive">Indica si la sede está activa.</param>
public sealed record EstablishmentBranchListItemResponse(
    Guid Id,
    Guid EstablishmentId,
    string Name,
    string FullAddress,
    string? City,
    string? Province,
    decimal? Latitude,
    decimal? Longitude,
    decimal RatingAverage,
    int TotalReviews,
    bool IsActive);
