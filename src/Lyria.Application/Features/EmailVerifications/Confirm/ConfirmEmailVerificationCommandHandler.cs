using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.EmailVerifications.Confirm;

/// <summary>
/// Confirma el correo del usuario con el código recibido y activa la cuenta.
/// </summary>
/// <remarks>
/// Todos los rechazos devuelven <see cref="EmailVerificationErrors.InvalidCode"/>: un
/// correo inexistente, un código incorrecto, vencido, revocado o ya usado, los intentos
/// agotados, una cuenta ya verificada y una cuenta en un estado no permitido son
/// indistinguibles entre sí.
///
/// La activación —marcar el código como usado, establecer <c>IsEmailVerified</c> y pasar
/// el estado a <see cref="UserStatus.Active"/>— se confirma en una única transacción. Dos
/// confirmaciones simultáneas no pueden canjear el mismo código dos veces: la segunda no
/// encuentra la fila en el estado que esperaba y no confirma nada.
/// </remarks>
public sealed class ConfirmEmailVerificationCommandHandler(
    IUserRepository userRepository,
    IUserEmailVerificationRepository verificationRepository,
    IEmailVerificationWriter verificationWriter,
    IEmailVerificationCodeHasher codeHasher,
    IEmailVerificationDefaults defaults,
    TimeProvider timeProvider,
    ILogger<ConfirmEmailVerificationCommandHandler> logger)
    : ICommandHandler<ConfirmEmailVerificationCommand>
{
    public async ValueTask<Result> Handle(
        ConfirmEmailVerificationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result invalidCode = Result.Failure(EmailVerificationErrors.InvalidCode());

        // 1. Normalización con las mismas reglas que usa el registro.
        string normalizedEmail = User.NormalizeEmail(command.Email);

        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.IsEmailVerified || user.Status != UserStatus.Unverified)
        {
            EmailVerificationLog.ConfirmationFailed(logger);

            return invalidCode;
        }

        // 2. Última verificación ni usada ni revocada.
        UserEmailVerification? verification =
            await verificationRepository.GetLatestPendingAsync(user.Id, cancellationToken);

        if (verification is null)
        {
            EmailVerificationLog.ConfirmationFailed(logger);

            return invalidCode;
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // 3. Un código vencido o con los intentos agotados no admite más intentos: el
        //    contador no se incrementa y la respuesta es la misma.
        if (verification.IsExpired(utcNow) ||
            verification.HasExceededMaximumAttempts(defaults.MaximumFailedAttempts))
        {
            EmailVerificationLog.ConfirmationFailed(logger);

            return invalidCode;
        }

        // 4. Comparación del código contra el hash almacenado, en tiempo constante.
        if (!codeHasher.Matches(verification.CodeHash, command.Code))
        {
            verification.RegisterFailedAttempt();

            // Al agotar los intentos el código deja de ser utilizable de forma explícita:
            // solicitar uno nuevo pasa a ser la única salida.
            if (verification.HasExceededMaximumAttempts(defaults.MaximumFailedAttempts))
            {
                verification.Revoke(utcNow);
            }

            await verificationWriter.RegisterFailedAttemptAsync(verification, cancellationToken);

            EmailVerificationLog.ConfirmationFailed(logger);

            return invalidCode;
        }

        // 5. Canje del código y activación de la cuenta, en una sola transacción.
        verification.MarkAsUsed(utcNow);
        user.MarkEmailAsVerified();
        user.ChangeStatus(UserStatus.Active);

        bool confirmed = await verificationWriter.TryConfirmAsync(
            user, verification, cancellationToken);

        if (!confirmed)
        {
            // Otra solicitud simultánea canjeó el código primero. Nada quedó confirmado.
            EmailVerificationLog.ConfirmationFailed(logger);

            return invalidCode;
        }

        EmailVerificationLog.ConfirmationSucceeded(logger, user.Id.Value);

        return Result.Success();
    }
}
