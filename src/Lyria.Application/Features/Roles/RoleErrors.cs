using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Roles;

public static class RoleErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Roles.NotFound",
            $"No se encontró el rol con ID '{id}'.");

    public static Error CodeAlreadyExists() =>
        Error.Conflict(
            "Roles.CodeAlreadyExists",
            "Ya existe un rol con ese código.");
}
