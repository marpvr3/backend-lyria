using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.EstablishmentBranchServices;

public static class EstablishmentBranchServiceErrors
{
    public static Error NotFound(Guid branchId, Guid serviceId) =>
        Error.NotFound(
            "EstablishmentBranchService.NotFound",
            "No se encontró la asociación entre la sede y el servicio.");

    public static Error AlreadyExists(Guid branchId, Guid serviceId) =>
        Error.Conflict(
            "EstablishmentBranchService.AlreadyExists",
            "El servicio ya está asignado a la sede indicada.");

    public static Error BranchNotFound(Guid branchId) =>
        Error.NotFound(
            "EstablishmentBranch.NotFound",
            "No se encontró la sede indicada.");

    public static Error BranchInactive(Guid branchId) =>
        Error.Conflict(
            "EstablishmentBranch.Inactive",
            "La sede indicada se encuentra inactiva.");

    public static Error ServiceNotFound(Guid serviceId) =>
        Error.NotFound(
            "Service.NotFound",
            "No se encontró el servicio indicado.");

    public static Error ServiceInactive(Guid serviceId) =>
        Error.Conflict(
            "Service.Inactive",
            "El servicio indicado se encuentra inactivo.");
}
