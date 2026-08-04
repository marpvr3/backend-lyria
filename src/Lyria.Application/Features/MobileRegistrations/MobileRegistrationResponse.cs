namespace Lyria.Application.Features.MobileRegistrations;

/// <summary>
/// Resultado del registro de un usuario desde la aplicación móvil.
/// </summary>
/// <param name="UserId">Identificador del usuario creado.</param>
/// <param name="Name">Nombre normalizado del usuario.</param>
/// <param name="LastName">Apellido normalizado del usuario.</param>
/// <param name="Email">Correo electrónico normalizado del usuario.</param>
/// <param name="Status">Estado inicial del usuario. Siempre 'Unverified'.</param>
/// <param name="RestrictionIds">Restricciones alimenticias asociadas al usuario.</param>
/// <remarks>
/// No expone contraseña, hash de contraseña, rol interno ni detalles de la transacción.
/// </remarks>
public sealed record MobileRegistrationResponse(
    Guid UserId,
    string Name,
    string LastName,
    string Email,
    string Status,
    IReadOnlyList<Guid> RestrictionIds);
