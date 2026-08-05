using Lyria.Domain.Users;

namespace Lyria.Application.Features.Authentication;

/// <summary>
/// Reglas transversales del flujo de autenticación móvil.
/// </summary>
public static class AuthenticationPolicy
{
    /// <summary>
    /// Esquema de autorización que la API emite y espera.
    /// </summary>
    public const string TokenType = "Bearer";

    /// <summary>
    /// Indica si una cuenta en el estado indicado puede autenticarse.
    /// </summary>
    /// <remarks>
    /// Mientras no exista confirmación de correo, <see cref="UserStatus.Unverified"/>
    /// es un estado operativo: es el estado con el que el registro móvil crea las
    /// cuentas y debe poder iniciar sesión. <see cref="UserStatus.Suspended"/> y
    /// <see cref="UserStatus.Deleted"/> quedan excluidos.
    /// </remarks>
    public static bool AllowsAuthentication(UserStatus status) =>
        status is UserStatus.Unverified or UserStatus.Active;
}
