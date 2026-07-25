namespace Lyria.Application.Features.BranchImages;

/// <summary>
/// Respuesta con las imágenes de una sede.
/// </summary>
/// <param name="BranchId">Identificador de la sede.</param>
/// <param name="Images">Lista de imágenes.</param>
public sealed record BranchImagesResponse(
    Guid BranchId,
    IReadOnlyList<BranchImageResponse> Images);

/// <summary>
/// Imagen de una sede.
/// </summary>
/// <param name="Id">Identificador de la imagen.</param>
/// <param name="Url">URL de la imagen.</param>
/// <param name="FileName">Nombre del archivo.</param>
/// <param name="AlternativeText">Texto alternativo.</param>
/// <param name="IsPrimary">Indica si es la imagen principal.</param>
/// <param name="SortOrder">Orden de presentación.</param>
/// <param name="IsActive">Indica si la imagen está activa.</param>
public sealed record BranchImageResponse(
    Guid Id,
    string Url,
    string FileName,
    string? AlternativeText,
    bool IsPrimary,
    int SortOrder,
    bool IsActive);
