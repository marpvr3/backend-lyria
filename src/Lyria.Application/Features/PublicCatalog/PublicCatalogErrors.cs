using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.PublicCatalog;

public static class PublicCatalogErrors
{
    public static Error EstablishmentNotFound(string slug) =>
        Error.NotFound(
            "PublicCatalog.EstablishmentNotFound",
            $"No se encontró el establecimiento con slug '{slug}'.");

    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "PublicCatalog.BranchNotFound",
            $"No se encontró la sede con ID '{branchId}'.");

    public static readonly Error InvalidPage =
        Error.Validation(
            "PublicCatalog.InvalidPage",
            "La página debe ser al menos 1.");

    public static readonly Error InvalidPageSizeMin =
        Error.Validation(
            "PublicCatalog.InvalidPageSize",
            "El tamaño de página debe ser al menos 1.");

    public static readonly Error InvalidPageSizeMax =
        Error.Validation(
            "PublicCatalog.InvalidPageSize",
            "El tamaño de página no puede superar 100.");

    public static Error InvalidSortBy(string value) =>
        Error.Validation(
            "PublicCatalog.InvalidSortBy",
            $"El criterio de ordenamiento '{value}' no es válido. Valores permitidos: name, newest.");

    public static Error InvalidSortDirection(string value) =>
        Error.Validation(
            "PublicCatalog.InvalidSortDirection",
            $"La dirección de ordenamiento '{value}' no es válida. Valores permitidos: asc, desc.");

    public static Error InvalidComplianceLevel(int value) =>
        Error.Validation(
            "PublicCatalog.InvalidComplianceLevel",
            $"El nivel de cumplimiento '{value}' no es válido. Valores permitidos: 1 (Garantizado), 2 (Parcial), 3 (Bajo solicitud).");
}
