using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.EmailVerifications;

/// <summary>
/// Mensajes de registro del flujo de verificación de correo, generados con
/// <see cref="LoggerMessageAttribute"/>.
/// </summary>
/// <remarks>
/// Ningún mensaje admite datos sensibles: no se registran el código, su hash, el secreto
/// con el que se calcula, el correo del destinatario, el cuerpo del mensaje ni las
/// credenciales del proveedor SMTP. El identificador del usuario sí se registra: es un
/// dato interno que ya aparece en el resto de los logs de la aplicación y permite
/// diagnosticar sin exponer la cuenta.
/// </remarks>
public static partial class EmailVerificationLog
{
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Error,
        Message =
            "No se pudo enviar el correo de verificación del usuario {UserId}. " +
            "El usuario permanece pendiente de verificación y puede solicitar un reenvío.")]
    public static partial void SendFailed(ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Se emitió un nuevo código de verificación para el usuario {UserId}.")]
    public static partial void CodeIssued(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Intento de confirmación de correo no válido.")]
    public static partial void ConfirmationFailed(ILogger logger);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Information,
        Message = "El usuario {UserId} confirmó su correo electrónico.")]
    public static partial void ConfirmationSucceeded(ILogger logger, Guid userId);
}
