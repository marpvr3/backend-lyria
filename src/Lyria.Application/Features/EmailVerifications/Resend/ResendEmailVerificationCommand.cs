using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EmailVerifications.Resend;

/// <summary>
/// Solicita un nuevo código de verificación para el correo indicado.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
public sealed record ResendEmailVerificationCommand(string Email)
    : ICommand<EmailVerificationResendResponse>;
