using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions;

public static class EstablishmentBranchRestrictionErrors
{
    public static Error NotFound(Guid branchId, Guid restrictionId) =>
        Error.NotFound(
            "EstablishmentBranchRestriction.NotFound",
            "No se encontró la asociación entre la sede y la restricción.");

    public static Error AlreadyExists(Guid branchId, Guid restrictionId) =>
        Error.Conflict(
            "EstablishmentBranchRestriction.AlreadyExists",
            "La restricción ya está asignada a la sede indicada.");

    public static Error InvalidComplianceLevel() =>
        Error.Validation(
            "EstablishmentBranchRestriction.InvalidComplianceLevel",
            "El nivel de cumplimiento indicado no es válido.");

    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error BranchInactive(Guid branchId) =>
        Error.Conflict(
            "EstablishmentBranch.Inactive",
            "La sede indicada se encuentra inactiva.");

    public static Error RestrictionNotFound(Guid restrictionId) =>
        Error.NotFound(
            "Restriction.NotFound",
            "No se encontró la restricción indicada.");

    public static Error RestrictionInactive(Guid restrictionId) =>
        Error.Conflict(
            "Restriction.Inactive",
            "La restricción indicada se encuentra inactiva.");
}
