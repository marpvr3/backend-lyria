using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions;

/// <summary>
/// Detalle completo de una asociación entre sede y restricción.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="RestrictionId">Identificador de la restricción.</param>
/// <param name="RestrictionName">Nombre de la restricción.</param>
/// <param name="RestrictionDescription">Descripción de la restricción.</param>
/// <param name="ComplianceLevel">Nivel de cumplimiento: 1 — Garantizado, 2 — Parcial, 3 — Bajo solicitud.</param>
/// <param name="IsCertified">Indica si la sede declara contar con una certificación o respaldo formal.</param>
/// <param name="Observation">Observación opcional sobre la asociación.</param>
/// <param name="IsActive">Indica si la asociación entre la sede y la restricción continúa vigente.</param>
/// <param name="CreatedAtUtc">Fecha de creación.</param>
/// <param name="UpdatedAtUtc">Fecha de última actualización.</param>
public sealed record EstablishmentBranchRestrictionResponse(
    Guid BranchId,
    Guid RestrictionId,
    string RestrictionName,
    string? RestrictionDescription,
    RestrictionComplianceLevel ComplianceLevel,
    bool IsCertified,
    string? Observation,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
