namespace Lyria.Application.Features.EstablishmentBranches;

/// <summary>
/// Detalle completo de una sede.
/// </summary>
/// <param name="Id">Identificador único de la sede.</param>
/// <param name="EstablishmentId">Identificador del establecimiento al que pertenece la sede.</param>
/// <param name="Name">Nombre de la sede.</param>
/// <param name="Street">Calle de la sede.</param>
/// <param name="Number">Número de la dirección.</param>
/// <param name="AddressComplement">Complemento de dirección (piso, local, etc.).</param>
/// <param name="Neighborhood">Barrio de la sede.</param>
/// <param name="City">Ciudad de la sede.</param>
/// <param name="Province">Provincia de la sede.</param>
/// <param name="PostalCode">Código postal de la sede.</param>
/// <param name="Country">País de la sede.</param>
/// <param name="FullAddress">Dirección completa formateada.</param>
/// <param name="Latitude">Latitud geográfica de la sede.</param>
/// <param name="Longitude">Longitud geográfica de la sede.</param>
/// <param name="Phone">Teléfono de contacto de la sede.</param>
/// <param name="WhatsApp">WhatsApp de contacto de la sede.</param>
/// <param name="Email">Correo electrónico de contacto de la sede.</param>
/// <param name="RatingAverage">Calificación promedio de la sede.</param>
/// <param name="TotalReviews">Cantidad total de reseñas de la sede.</param>
/// <param name="IsActive">Indica si la sede está activa.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC en que se creó el registro.</param>
/// <param name="UpdatedAtUtc">Fecha y hora UTC de la última modificación.</param>
public sealed record EstablishmentBranchResponse(
    Guid Id,
    Guid EstablishmentId,
    string Name,
    string Street,
    string? Number,
    string? AddressComplement,
    string? Neighborhood,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    string FullAddress,
    decimal? Latitude,
    decimal? Longitude,
    string? Phone,
    string? WhatsApp,
    string? Email,
    decimal RatingAverage,
    int TotalReviews,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
