using System.Collections.Concurrent;
using Lyria.Application.Abstractions.Notifications;

namespace Lyria.Api.FunctionalTests;

/// <summary>
/// Doble del remitente de correo para las pruebas funcionales.
/// </summary>
/// <remarks>
/// Se registra únicamente en el host de pruebas y sustituye al adaptador SMTP: ninguna
/// prueba envía correo real ni necesita configuración SMTP. Conserva el código enviado
/// para que las pruebas puedan confirmarlo, algo que la API nunca expone.
/// </remarks>
internal sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sentEmails = new();

    /// <summary>
    /// Cuando se establece, el envío falla. Simula un problema del proveedor.
    /// </summary>
    public Exception? FailureToThrow { get; set; }

    public IReadOnlyCollection<SentEmail> SentEmails => [.. _sentEmails];

    /// <summary>
    /// Último código enviado a la dirección indicada, o <c>null</c> si no se envió ninguno.
    /// </summary>
    public string? LastCodeFor(string recipientEmail) =>
        _sentEmails
            .Where(e => string.Equals(
                e.RecipientEmail, recipientEmail, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.VerificationCode)
            .LastOrDefault();

    public Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (FailureToThrow is not null)
        {
            return Task.FromException(FailureToThrow);
        }

        _sentEmails.Enqueue(new SentEmail(
            recipientEmail, recipientName, verificationCode, expiresAtUtc));

        return Task.CompletedTask;
    }

    internal sealed record SentEmail(
        string RecipientEmail,
        string RecipientName,
        string VerificationCode,
        DateTime ExpiresAtUtc);
}
