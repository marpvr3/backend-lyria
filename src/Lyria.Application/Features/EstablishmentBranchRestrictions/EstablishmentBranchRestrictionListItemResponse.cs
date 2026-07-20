using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions;

/// <summary>
/// Elemento de lista de restricciones asignadas a una sede.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="RestrictionId">Identificador de la restricción.</param>
/// <param name="RestrictionName">Nombre de la restricción.</param>
/// <param name="ComplianceLevel">Nivel de cumplimiento: 1 — Garantizado, 2 — Parcial, 3 — Bajo solicitud.</param>
/// <param name="IsCertified">Indica si la sede declara contar con una certificación o respaldo formal.</param>
/// <param name="Observation">Observación opcional.</param>
/// <param name="IsActive">Indica si la asociación está vigente.</param>
public sealed record EstablishmentBranchRestrictionListItemResponse(
    Guid BranchId,
    Guid RestrictionId,
    string RestrictionName,
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation,
    bool IsActive);
