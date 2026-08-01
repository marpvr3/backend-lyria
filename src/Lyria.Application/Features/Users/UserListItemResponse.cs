namespace Lyria.Application.Features.Users;

/// <summary>
/// Resumen de un usuario para listados paginados.
/// </summary>
/// <param name="Id">Identificador único del usuario.</param>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Status">Estado actual del usuario.</param>
/// <param name="IsEmailVerified">Indica si el correo electrónico ha sido verificado.</param>
public sealed record UserListItemResponse(
    Guid Id,
    string Name,
    string LastName,
    string Email,
    string Status,
    bool IsEmailVerified);
