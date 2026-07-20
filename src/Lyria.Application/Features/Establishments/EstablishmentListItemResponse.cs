namespace Lyria.Application.Features.Establishments;

/// <summary>
/// Resumen de un establecimiento para listados paginados.
/// </summary>
/// <param name="Id">Identificador único del establecimiento.</param>
/// <param name="CategoryId">Identificador de la categoría.</param>
/// <param name="CategoryName">Nombre de la categoría.</param>
/// <param name="Name">Nombre del establecimiento.</param>
/// <param name="Slug">Slug único para la URL.</param>
/// <param name="IsVerified">Indica si el establecimiento ha sido verificado.</param>
/// <param name="IsActive">Indica si el establecimiento está activo.</param>
public sealed record EstablishmentListItemResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    bool IsVerified,
    bool IsActive);
