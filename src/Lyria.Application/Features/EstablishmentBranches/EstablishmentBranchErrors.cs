using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.EstablishmentBranches;

public static class EstablishmentBranchErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            $"No se encontró la sede con ID '{id}'.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict(
            "EstablishmentBranch.NameAlreadyExists",
            "Ya existe una sede con ese nombre para el establecimiento indicado.");

    public static Error InvalidCoordinates() =>
        Error.Validation(
            "EstablishmentBranch.InvalidCoordinates",
            "Las coordenadas proporcionadas no son válidas. Ambas deben informarse o ambas deben ser nulas.");

    public static Error EstablishmentNotFound(Guid id) =>
        Error.NotFound(
            "Establishment.NotFound",
            $"No se encontró el establecimiento con ID '{id}'.");

    public static Error EstablishmentInactive(Guid id) =>
        Error.Validation(
            "Establishment.Inactive",
            $"El establecimiento con ID '{id}' no está activo.");
}
