namespace Lyria.Application.Features.EstablishmentBranchServices;

/// <summary>
/// Detalle completo de una asociación entre sede y servicio.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="ServiceId">Identificador del servicio.</param>
/// <param name="ServiceName">Nombre del servicio.</param>
/// <param name="ServiceDescription">Descripción del servicio.</param>
/// <param name="ServiceIconUrl">URL del icono del servicio.</param>
/// <param name="IsAvailable">Indica si el servicio se ofrece actualmente en la sede.</param>
/// <param name="Observation">Observación opcional sobre la asociación.</param>
/// <param name="IsActive">Indica si la asociación entre la sede y el servicio está vigente.</param>
/// <param name="CreatedAtUtc">Fecha de creación.</param>
/// <param name="UpdatedAtUtc">Fecha de última actualización.</param>
public sealed record EstablishmentBranchServiceResponse(
    Guid BranchId,
    Guid ServiceId,
    string ServiceName,
    string? ServiceDescription,
    string? ServiceIconUrl,
    bool IsAvailable,
    string? Observation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
