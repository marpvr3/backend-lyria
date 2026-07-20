using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Restrictions;

public static class RestrictionErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Restriction.NotFound",
            "No se encontró la restricción indicada.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Restriction.NameAlreadyExists",
            "Ya existe una restricción con el nombre indicado.");
}
