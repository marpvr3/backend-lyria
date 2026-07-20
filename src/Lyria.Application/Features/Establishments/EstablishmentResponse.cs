namespace Lyria.Application.Features.Establishments;

/// <summary>
/// Detalle completo de un establecimiento.
/// </summary>
/// <param name="Id">Identificador único del establecimiento.</param>
/// <param name="CategoryId">Identificador de la categoría.</param>
/// <param name="CategoryName">Nombre de la categoría.</param>
/// <param name="Name">Nombre del establecimiento.</param>
/// <param name="Slug">Slug único para la URL.</param>
/// <param name="Description">Descripción del establecimiento.</param>
/// <param name="Website">Sitio web del establecimiento.</param>
/// <param name="Instagram">Cuenta de Instagram.</param>
/// <param name="LogoUrl">URL opcional del logotipo oficial del establecimiento.</param>
/// <param name="ContactEmail">Correo electrónico opcional de contacto del establecimiento.</param>
/// <param name="ContactPhone">Número telefónico opcional de contacto del establecimiento.</param>
/// <param name="IsVerified">Indica si el establecimiento ha sido verificado.</param>
/// <param name="VerifiedAtUtc">Fecha y hora UTC en que el establecimiento fue verificado. Es nula mientras no esté verificado.</param>
/// <param name="IsActive">Indica si el establecimiento está activo.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC en que se creó el registro.</param>
/// <param name="UpdatedAtUtc">Fecha y hora UTC de la última modificación. Es nula cuando el registro nunca fue modificado.</param>
public sealed record EstablishmentResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? Instagram,
    string? LogoUrl,
    string? ContactEmail,
    string? ContactPhone,
    bool IsVerified,
    DateTime? VerifiedAtUtc,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
