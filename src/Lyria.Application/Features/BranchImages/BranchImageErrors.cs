using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.BranchImages;

public static class BranchImageErrors
{
    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound("EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error NotFound(Guid imageId) =>
        Error.NotFound("BranchImage.NotFound",
            "No se encontró la imagen indicada.");

    public static Error DoesNotBelongToBranch(Guid imageId) =>
        Error.NotFound("BranchImage.NotFound",
            "No se encontró la imagen indicada.");

    public static Error Inactive(Guid imageId) =>
        Error.Conflict("BranchImage.Inactive",
            "La imagen indicada se encuentra inactiva.");

    public static Error InvalidUrl() =>
        Error.Validation("BranchImage.InvalidUrl",
            "La URL de la imagen no es válida.");

    public static Error InvalidFileName() =>
        Error.Validation("BranchImage.InvalidFileName",
            "El nombre del archivo no es válido.");

    public static Error DuplicateImageOrderEntry() =>
        Error.Validation("BranchImage.DuplicateImageOrderEntry",
            "La lista de reordenamiento contiene identificadores de imagen repetidos.");

    public static Error InvalidSortOrder() =>
        Error.Validation("BranchImage.InvalidSortOrder",
            "El orden de la imagen no puede ser negativo.");

    public static Error ImageNotFromBranch(Guid imageId) =>
        Error.NotFound("BranchImage.NotFound",
            "No se encontró la imagen indicada.");
}
