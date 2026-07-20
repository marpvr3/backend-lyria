using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Establishments;

public static class EstablishmentErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Establishments.NotFound",
            $"No se encontró el establecimiento con ID '{id}'.");

    public static Error SlugAlreadyExists(string slug) =>
        Error.Conflict(
            "Establishments.SlugAlreadyExists",
            $"Ya existe un establecimiento con el slug '{slug}'.");

    public static Error CategoryNotAvailable(Guid categoryId) =>
        Error.Validation(
            "Establishments.CategoryNotAvailable",
            $"La categoría con ID '{categoryId}' no está disponible. Verifique que exista y esté activa.");
}
