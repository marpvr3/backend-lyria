using Lyria.Application.Common.Errors;

namespace Lyria.Application.Features.EmailVerifications;

/// <summary>
/// Errores del flujo de verificación de correo.
/// </summary>
/// <remarks>
/// Existe un único error de confirmación, deliberadamente genérico. Un correo
/// inexistente, un código incorrecto, vencido, revocado o ya usado, los intentos
/// agotados, un usuario ya verificado y un usuario en un estado no permitido producen
/// exactamente el mismo error, de modo que la respuesta no permita deducir si una cuenta
/// existe ni en qué situación se encuentra.
/// </remarks>
public static class EmailVerificationErrors
{
    /// <summary>
    /// Único error de confirmación. No distingue causa.
    /// </summary>
    public static Error InvalidCode() =>
        Error.Validation(
            "EmailVerification.InvalidCode",
            "El código de verificación no es válido o ha vencido.");
}
