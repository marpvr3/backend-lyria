namespace Lyria.Application.Features.UserRoles;

/// <summary>
/// Detalle de una asignación de rol a un usuario.
/// </summary>
/// <param name="Id">Identificador único de la asignación.</param>
/// <param name="UserId">Identificador del usuario.</param>
/// <param name="RoleId">Identificador del rol.</param>
/// <param name="RoleName">Nombre del rol asignado.</param>
/// <param name="ScopeType">Tipo de alcance de la asignación.</param>
/// <param name="EstablishmentId">Identificador del establecimiento asociado al alcance.</param>
/// <param name="BranchId">Identificador de la sede asociada al alcance.</param>
/// <param name="IsActive">Indica si la asignación está activa.</param>
/// <param name="AssignedAtUtc">Fecha y hora UTC en que se realizó la asignación.</param>
/// <param name="EndedAtUtc">Fecha y hora UTC en que finalizó la asignación.</param>
public sealed record UserRoleResponse(
    Guid Id,
    Guid UserId,
    Guid RoleId,
    string RoleName,
    string ScopeType,
    Guid? EstablishmentId,
    Guid? BranchId,
    bool IsActive,
    DateTime AssignedAtUtc,
    DateTime? EndedAtUtc);
