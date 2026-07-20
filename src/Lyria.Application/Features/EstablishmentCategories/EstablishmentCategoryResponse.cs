namespace Lyria.Application.Features.EstablishmentCategories;

/// <summary>
/// Detalle de una categoría de establecimientos.
/// </summary>
/// <param name="Id">Identificador único de la categoría.</param>
/// <param name="Name">Nombre de la categoría.</param>
/// <param name="Description">Descripción opcional de la categoría.</param>
/// <param name="IconUrl">URL opcional del ícono representativo de la categor��a.</param>
/// <param name="SortOrder">Orden de prioridad para mostrar la categoría.</param>
/// <param name="IsActive">Indica si la categoría está activa.</param>
public sealed record EstablishmentCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    int SortOrder,
    bool IsActive);
