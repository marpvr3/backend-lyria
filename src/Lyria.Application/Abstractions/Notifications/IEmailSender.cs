namespace Lyria.Application.Abstractions.Notifications;

/// <summary>
/// Envío de correo electrónico transaccional.
/// </summary>
/// <remarks>
/// La abstracción es independiente del proveedor: no expone tipos SMTP, direcciones de
/// servidor, credenciales ni plantillas. La composición del mensaje y la conexión con el
/// proveedor viven íntegramente en Infrastructure.
///
/// El envío nunca ocurre dentro de una transacción de base de datos: los casos de uso
/// confirman primero y envían después.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Envía al usuario el código con el que confirma su correo electrónico.
    /// </summary>
    /// <param name="recipientEmail">
    /// Correo del usuario. Es el único destino admitido del código.
    /// </param>
    /// <param name="recipientName">Nombre del usuario, para el saludo del mensaje.</param>
    /// <param name="verificationCode">
    /// Código de seis dígitos en claro. No debe registrarse en logs ni devolverse por la API.
    /// </param>
    /// <param name="expiresAtUtc">Instante en el que el código deja de ser válido.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}
