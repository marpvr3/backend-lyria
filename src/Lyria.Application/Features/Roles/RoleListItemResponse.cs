namespace Lyria.Application.Features.Roles;

/// <summary>
/// Resumen de un rol para listados paginados.
/// </summary>
/// <param name="Id">Identificador único del rol.</param>
/// <param name="Code">Código único del rol.</param>
/// <param name="Name">Nombre del rol.</param>
/// <param name="IsActive">Indica si el rol está activo.</param>
public sealed record RoleListItemResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);
