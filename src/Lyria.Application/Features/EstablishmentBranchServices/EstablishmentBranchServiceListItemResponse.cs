namespace Lyria.Application.Features.EstablishmentBranchServices;

/// <summary>
/// Elemento de lista de servicios asignados a una sede.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="ServiceId">Identificador del servicio.</param>
/// <param name="ServiceName">Nombre del servicio.</param>
/// <param name="ServiceIconUrl">URL del icono del servicio.</param>
/// <param name="IsAvailable">Indica si el servicio se ofrece actualmente en la sede.</param>
/// <param name="Observation">Observación opcional.</param>
/// <param name="IsActive">Indica si la asociación está vigente.</param>
public sealed record EstablishmentBranchServiceListItemResponse(
    Guid BranchId,
    Guid ServiceId,
    string ServiceName,
    string? ServiceIconUrl,
    bool IsAvailable,
    string? Observation,
    bool IsActive);
