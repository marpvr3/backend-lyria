using Lyria.Application.Common.Errors;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.Features.UserRestrictions;

public static class UserRestrictionErrors
{
    public static Error NotFound(Guid userId, Guid restrictionId) =>
        Error.NotFound(
            "UserRestrictions.NotFound",
            "No se encontró la asociación entre el usuario y la restricción.");

    public static Error AlreadyExists(Guid userId, Guid restrictionId) =>
        Error.Conflict(
            "UserRestrictions.AlreadyExists",
            "La restricción ya está asignada al usuario indicado.");

    public static Error UserNotFound(Guid userId) =>
        Error.NotFound(
            "UserRestrictions.UserNotFound",
            $"No se encontró el usuario con ID '{userId}'.");

    public static Error RestrictionNotFound(Guid restrictionId) =>
        Error.NotFound(
            "UserRestrictions.RestrictionNotFound",
            $"No se encontró la restricción con ID '{restrictionId}'.");

    public static Error InvalidImportanceLevel() =>
        Error.Validation(
            "UserRestrictions.InvalidImportanceLevel",
            "El nivel de importancia indicado no es válido. " +
            $"Valores permitidos: {string.Join(", ", UserRestrictionImportanceLevels.All)}.");
}
