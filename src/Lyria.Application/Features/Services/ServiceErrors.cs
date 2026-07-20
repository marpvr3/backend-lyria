using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Services;

public static class ServiceErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Service.NotFound",
            "No se encontró el servicio indicado.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Service.NameAlreadyExists",
            "Ya existe un servicio con el nombre indicado.");
}
