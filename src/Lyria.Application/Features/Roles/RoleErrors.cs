using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Roles;

public static class RoleErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Roles.NotFound",
            $"No se encontró el rol con ID '{id}'.");
}
