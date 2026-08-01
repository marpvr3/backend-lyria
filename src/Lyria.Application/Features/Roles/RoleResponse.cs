namespace Lyria.Application.Features.Roles;

/// <summary>
/// Detalle completo de un rol.
/// </summary>
/// <param name="Id">Identificador único del rol.</param>
/// <param name="Code">Código único del rol.</param>
/// <param name="Name">Nombre del rol.</param>
/// <param name="Description">Descripción opcional del rol.</param>
/// <param name="IsActive">Indica si el rol está activo.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC en que se creó el registro.</param>
/// <param name="UpdatedAtUtc">Fecha y hora UTC de la última modificación.</param>
public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
