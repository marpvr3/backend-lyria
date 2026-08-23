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
    /// Solo <see cref="UserStatus.Active"/> puede iniciar sesión. Desde que existe la
    /// confirmación de correo, <see cref="UserStatus.Unverified"/> dejó de ser un estado
    /// operativo: es el estado transitorio con el que el registro móvil crea la cuenta
    /// hasta que el usuario canjea su código. <see cref="UserStatus.Suspended"/> y
    /// <see cref="UserStatus.Deleted"/> también quedan excluidos.
    ///
    /// El rechazo de una cuenta sin verificar es indistinguible de una credencial
    /// inválida: un mensaje del tipo "debe verificar su correo" revelaría que la cuenta
    /// existe.
    /// </remarks>
    public static bool AllowsAuthentication(UserStatus status) =>
        status is UserStatus.Active;
}
