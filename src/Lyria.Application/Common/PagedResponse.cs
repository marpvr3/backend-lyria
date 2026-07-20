namespace Lyria.Application.Common;

/// <summary>
/// Respuesta paginada genérica.
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la página.</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="Page">Número de página actual.</param>
/// <param name="PageSize">Cantidad de elementos por página.</param>
/// <param name="TotalItems">Cantidad total de elementos en todas las páginas.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    /// <summary>
    /// Cantidad total de páginas disponibles.
    /// </summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalItems / PageSize)
        : 0;
}
