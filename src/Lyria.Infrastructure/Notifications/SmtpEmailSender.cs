using Lyria.Application.Abstractions.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Lyria.Infrastructure.Notifications;

/// <summary>
/// Adaptador SMTP del envío de correo, construido sobre MailKit.
/// </summary>
/// <remarks>
/// Es el único punto del sistema que conoce el proveedor: ni Application ni Api ven tipos
/// SMTP. El mensaje se compone con la plantilla centralizada, en texto plano y HTML.
///
/// Nada de lo que pasa por aquí se registra: ni la contraseña SMTP, ni el código, ni el
/// cuerpo del mensaje, ni la dirección completa del destinatario. Los fallos se propagan
/// como excepción y los registra el caso de uso, que solo anota el identificador del
/// usuario.
/// </remarks>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = EmailVerificationMessageTemplate.Subject;

        var body = new BodyBuilder
        {
            TextBody = EmailVerificationMessageTemplate.BuildTextBody(
                recipientName, verificationCode, expiresAtUtc),
            HtmlBody = EmailVerificationMessageTemplate.BuildHtmlBody(
                recipientName, verificationCode, expiresAtUtc)
        };

        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();

        // STARTTLS cuando se exige TLS; en caso contrario se deja al servidor decidir
        // entre texto plano y una conexión ya cifrada. Sin TLS las credenciales viajarían
        // en claro, por lo que UseTls debe permanecer activo fuera de desarrollo local.
        SecureSocketOptions socketOptions = _options.UseTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(
            _options.SmtpHost, _options.SmtpPort, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(
                _options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    /// <summary>
    /// Comprueba la configuración mínima sin revelar sus valores.
    /// </summary>
    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException(
                "No se configuró 'Email:SmtpHost'. Defina la variable de entorno Email__SmtpHost.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException(
                "No se configuró 'Email:FromAddress'. Defina la variable de entorno Email__FromAddress.");
        }

        if (_options.SmtpPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException(
                "El puerto configurado en 'Email:SmtpPort' no es válido.");
        }
    }
}
