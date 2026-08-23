using Lyria.Application.Abstractions.Notifications;
using Lyria.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.EmailVerifications;

/// <summary>
/// Entrega del código de verificación al correo del usuario.
/// </summary>
/// <remarks>
/// Centraliza la única política admitida para el envío: se ejecuta siempre <b>después</b>
/// de que la transacción de base de datos haya quedado confirmada, y un fallo del
/// proveedor nunca revierte datos ya confirmados ni se propaga al cliente. El usuario
/// permanece pendiente de verificación y puede solicitar un reenvío.
///
/// El código viaja únicamente al correo del usuario y no se registra en ningún log.
/// </remarks>
internal static class EmailVerificationDelivery
{
    /// <summary>
    /// Envía el código y absorbe cualquier fallo del proveedor, registrándolo sin datos
    /// sensibles.
    /// </summary>
    public static async Task SendSafelyAsync(
        IEmailSender emailSender,
        ILogger logger,
        User user,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendEmailVerificationCodeAsync(
                user.Email,
                user.Name,
                verificationCode,
                expiresAtUtc,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // El registro y el reenvío ya quedaron confirmados: revertirlos aquí dejaría
            // datos inconsistentes por un problema ajeno a la base de datos.
            EmailVerificationLog.SendFailed(logger, user.Id.Value, exception);
        }
    }
}
