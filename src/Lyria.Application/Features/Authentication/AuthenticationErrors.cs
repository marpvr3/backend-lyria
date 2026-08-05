using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.Authentication;

/// <summary>
/// Errores del flujo de autenticación móvil.
/// </summary>
/// <remarks>
/// Son deliberadamente genéricos. Un correo inexistente, una contraseña incorrecta y
/// una cuenta suspendida o eliminada producen exactamente el mismo error, de modo que
/// la respuesta no permita deducir si una cuenta existe ni en qué estado se encuentra.
/// </remarks>
public static class AuthenticationErrors
{
    /// <summary>
    /// Único error de credenciales. No distingue causa.
    /// </summary>
    public static Error InvalidCredentials() =>
        Error.Unauthorized(
            "Authentication.InvalidCredentials",
            "El correo o la contraseña no son válidos.");

    /// <summary>
    /// Único error de refresh token. No distingue entre inexistente, vencido,
    /// revocado o reutilizado.
    /// </summary>
    public static Error InvalidRefreshToken() =>
        Error.Unauthorized(
            "Authentication.InvalidRefreshToken",
            "La sesión no es válida o ha expirado.");

    /// <summary>
    /// La solicitud no acredita una identidad válida.
    /// </summary>
    public static Error NotAuthenticated() =>
        Error.Unauthorized(
            "Authentication.NotAuthenticated",
            "La solicitud no está autenticada.");
}
