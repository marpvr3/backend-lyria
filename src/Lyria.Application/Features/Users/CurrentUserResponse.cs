namespace Lyria.Application.Features.Users;

/// <summary>
/// Perfil mínimo del usuario autenticado.
/// </summary>
/// <remarks>
/// Contiene únicamente lo que la aplicación móvil necesita hoy. No expone hash de
/// contraseña, sesiones, roles administrativos, favoritos, reseñas ni restricciones
/// alimenticias: esas últimas se incorporarán en un requerimiento posterior.
/// </remarks>
/// <param name="UserId">Identificador del usuario.</param>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Phone">Teléfono de contacto del usuario.</param>
/// <param name="BirthDate">Fecha de nacimiento del usuario.</param>
/// <param name="PhotoUrl">URL de la foto de perfil del usuario.</param>
/// <param name="Status">Estado actual de la cuenta.</param>
/// <param name="IsEmailVerified">Indica si el correo electrónico fue verificado.</param>
public sealed record CurrentUserResponse(
    Guid UserId,
    string Name,
    string LastName,
    string Email,
    string? Phone,
    DateOnly? BirthDate,
    string? PhotoUrl,
    string Status,
    bool IsEmailVerified);
