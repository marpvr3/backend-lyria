using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Notifications;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.EmailVerifications.Resend;

/// <summary>
/// Emite un nuevo código de verificación y lo envía al correo del usuario.
/// </summary>
/// <remarks>
/// La respuesta es siempre la misma —éxito con un mensaje genérico— sin importar si el
/// correo no existe, la cuenta ya está verificada, está suspendida o eliminada, o el
/// intervalo mínimo entre envíos aún no se cumplió. Solo un usuario pendiente de
/// verificación y fuera del intervalo mínimo recibe realmente un código nuevo.
///
/// El orden es estricto: se revocan los códigos vigentes y se inserta el nuevo dentro de
/// una única transacción; el correo se envía únicamente después de que esa transacción
/// haya quedado confirmada, para no mantener abierta una transacción SQL mientras se
/// conecta al proveedor.
/// </remarks>
public sealed class ResendEmailVerificationCommandHandler(
    IUserRepository userRepository,
    IUserEmailVerificationRepository verificationRepository,
    IEmailVerificationWriter verificationWriter,
    IEmailVerificationCodeGenerator codeGenerator,
    IEmailVerificationCodeHasher codeHasher,
    IEmailVerificationDefaults defaults,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<ResendEmailVerificationCommandHandler> logger)
    : ICommandHandler<ResendEmailVerificationCommand, EmailVerificationResendResponse>
{
    public async ValueTask<Result<EmailVerificationResendResponse>> Handle(
        ResendEmailVerificationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<EmailVerificationResendResponse> genericResponse =
            Result.Success(EmailVerificationResendResponse.Generic());

        // 1. Normalización con las mismas reglas que usa el registro.
        string normalizedEmail = User.NormalizeEmail(command.Email);

        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // 2. Solo una cuenta pendiente de verificación recibe un código. Cualquier otra
        //    situación devuelve exactamente la misma respuesta.
        if (user is null || user.IsEmailVerified || user.Status != UserStatus.Unverified)
        {
            return genericResponse;
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // 3. Intervalo mínimo entre envíos, persistido y por usuario. Es independiente de
        //    la limitación por IP y sobrevive a un cambio de dirección del atacante.
        DateTime? lastCreatedAtUtc = await verificationRepository.GetLastCreatedAtUtcAsync(
            user.Id, cancellationToken);

        if (lastCreatedAtUtc is not null &&
            utcNow < lastCreatedAtUtc.Value.AddSeconds(defaults.ResendCooldownSeconds))
        {
            return genericResponse;
        }

        // 4. Los códigos vigentes anteriores dejan de servir en cuanto se emite uno nuevo.
        IReadOnlyList<UserEmailVerification> pending =
            await verificationRepository.GetPendingAsync(user.Id, cancellationToken);

        foreach (UserEmailVerification verification in pending)
        {
            verification.Revoke(utcNow);
        }

        // 5. Código nuevo. Solo su hash llega a la base de datos.
        string code = codeGenerator.Generate();

        var created = UserEmailVerification.Create(
            UserEmailVerificationId.New(),
            user.Id,
            codeHasher.ComputeHash(code),
            utcNow,
            utcNow.AddMinutes(defaults.ExpirationMinutes));

        await verificationWriter.ResendAsync(pending, created, cancellationToken);

        EmailVerificationLog.CodeIssued(logger, user.Id.Value);

        // 6. Envío posterior a la confirmación de la transacción. Un fallo del proveedor
        //    no revierte nada ni cambia la respuesta.
        await EmailVerificationDelivery.SendSafelyAsync(
            emailSender, logger, user, code, created.ExpiresAtUtc, cancellationToken);

        return genericResponse;
    }
}
