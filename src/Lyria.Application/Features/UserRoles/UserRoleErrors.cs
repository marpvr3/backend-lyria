using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.UserRoles;

public static class UserRoleErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "UserRoles.NotFound",
            $"No se encontró la asignación de rol con ID '{id}'.");

    public static Error UserNotFound(Guid userId) =>
        Error.NotFound(
            "UserRoles.UserNotFound",
            $"No se encontró el usuario con ID '{userId}'.");

    public static Error RoleNotFound(Guid roleId) =>
        Error.NotFound(
            "UserRoles.RoleNotFound",
            $"No se encontró el rol con ID '{roleId}'.");

    public static Error EstablishmentNotFound(Guid establishmentId) =>
        Error.NotFound(
            "UserRoles.EstablishmentNotFound",
            $"No se encontró el establecimiento con ID '{establishmentId}'.");

    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "UserRoles.BranchNotFound",
            $"No se encontró la sede con ID '{branchId}'.");

    public static Error InvalidScope() =>
        Error.Validation(
            "UserRoles.InvalidScope",
            "El tipo de alcance proporcionado no es válido.");

    public static Error BranchDoesNotBelongToEstablishment() =>
        Error.Validation(
            "UserRoles.BranchDoesNotBelongToEstablishment",
            "La sede no pertenece al establecimiento indicado.");
}
