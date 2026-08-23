using System.Globalization;
using System.Net;

namespace Lyria.Infrastructure.Notifications;

/// <summary>
/// Plantilla del correo de verificación, en texto plano y HTML.
/// </summary>
/// <remarks>
/// La plantilla vive aquí y no en el caso de uso: el handler no construye marcado. El
/// nombre del usuario se escapa antes de insertarse en el HTML, de modo que un nombre con
/// caracteres de marcado no puede alterar la estructura del mensaje.
///
/// El mensaje contiene únicamente el saludo, el código, su vigencia y el aviso de que
/// puede ignorarse. No incluye contraseñas, tokens, enlaces ni datos personales
/// adicionales.
/// </remarks>
internal static class EmailVerificationMessageTemplate
{
    /// <summary>
    /// Asunto del mensaje.
    /// </summary>
    public const string Subject = "Verifica tu correo electrónico en Lyria";

    /// <summary>
    /// Compone la versión en texto plano.
    /// </summary>
    public static string BuildTextBody(
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc) =>
        $"""
         Hola {recipientName},

         Gracias por registrarte en Lyria.

         Tu código de verificación es:

         {verificationCode}

         Este código vence el {FormatExpiration(expiresAtUtc)} y solo puede utilizarse una vez.

         Si no realizaste este registro, puedes ignorar este mensaje.

         Equipo Lyria
         """;

    /// <summary>
    /// Compone la versión HTML. Escapa el nombre del destinatario.
    /// </summary>
    public static string BuildHtmlBody(
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc)
    {
        string safeName = WebUtility.HtmlEncode(recipientName);
        string safeCode = WebUtility.HtmlEncode(verificationCode);
        string safeExpiration = WebUtility.HtmlEncode(FormatExpiration(expiresAtUtc));

        return $"""
                <html lang="es">
                  <body style="font-family: Arial, Helvetica, sans-serif; color: #1f2933;">
                    <p>Hola {safeName},</p>
                    <p>Gracias por registrarte en Lyria.</p>
                    <p>Tu código de verificación es:</p>
                    <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">{safeCode}</p>
                    <p>Este código vence el {safeExpiration} y solo puede utilizarse una vez.</p>
                    <p>Si no realizaste este registro, puedes ignorar este mensaje.</p>
                    <p>Equipo Lyria</p>
                  </body>
                </html>
                """;
    }

    /// <summary>
    /// Expresa la vigencia como una fecha UTC explícita, para que el usuario no dependa
    /// del momento en que abra el mensaje.
    /// </summary>
    private static string FormatExpiration(DateTime expiresAtUtc) =>
        expiresAtUtc.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " UTC";
}
