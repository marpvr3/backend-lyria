using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.EstablishmentCategories;

public static class EstablishmentCategoryErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "EstablishmentCategories.NotFound",
            $"No se encontró la categoría de establecimiento con ID '{id}'.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict(
            "EstablishmentCategories.NameAlreadyExists",
            $"Ya existe una categoría de establecimiento con el nombre '{name}'.");
}
