namespace Lyria.Application.Features.EmailVerifications;

/// <summary>
/// Respuesta del reenvío de código de verificación.
/// </summary>
/// <remarks>
/// Es idéntica en todos los casos: correo inexistente, cuenta ya verificada, cuenta
/// suspendida o eliminada, intervalo mínimo aún no cumplido y envío efectivo. No revela
/// si la cuenta existe ni en qué estado está, y nunca contiene el código.
/// </remarks>
/// <param name="Message">Mensaje genérico para el usuario.</param>
public sealed record EmailVerificationResendResponse(string Message)
{
    /// <summary>
    /// Único mensaje que devuelve el reenvío.
    /// </summary>
    public const string GenericMessage =
        "Si existe una cuenta pendiente de verificación, se enviará un nuevo código.";

    /// <summary>
    /// Crea la respuesta genérica.
    /// </summary>
    public static EmailVerificationResendResponse Generic() => new(GenericMessage);
}
