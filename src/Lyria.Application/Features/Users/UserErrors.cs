using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Users;

public static class UserErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Users.NotFound",
            $"No se encontró el usuario con ID '{id}'.");

    public static Error EmailAlreadyExists() =>
        Error.Conflict(
            "Users.EmailAlreadyExists",
            "Ya existe un usuario con ese correo electrónico.");

    public static Error InvalidEmail() =>
        Error.Validation(
            "Users.InvalidEmail",
            "El correo electrónico no tiene un formato válido.");

    public static Error InvalidStatus() =>
        Error.Validation(
            "Users.InvalidStatus",
            "El estado proporcionado no es válido.");
}
