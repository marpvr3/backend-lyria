using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;

namespace Lyria.Application.Abstractions.Persistence;

/// <summary>
/// Coordinador de persistencia de la verificación de correo. Confirma cada operación
/// dentro de una única transacción de base de datos.
/// </summary>
/// <remarks>
/// No es un Unit of Work genérico: existe solo para este flujo y no expone control
/// transaccional. La transacción vive completamente en Infrastructure.
///
/// Ninguna de estas operaciones envía correo: el envío ocurre después de que la
/// transacción haya quedado confirmada.
/// </remarks>
public interface IEmailVerificationWriter
{
    /// <summary>
    /// Revoca las verificaciones vigentes del usuario y agrega la nueva de forma atómica.
    /// </summary>
    Task ResendAsync(
        IReadOnlyCollection<UserEmailVerification> revokedVerifications,
        UserEmailVerification createdVerification,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirma el canje del código junto con la activación del usuario.
    /// </summary>
    /// <returns>
    /// <c>true</c> si la confirmación quedó registrada; <c>false</c> si otra solicitud
    /// simultánea canjeó o invalidó el mismo código primero, en cuyo caso nada se
    /// confirma y el usuario no se activa dos veces.
    /// </returns>
    Task<bool> TryConfirmAsync(
        User user,
        UserEmailVerification verification,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste el intento fallido registrado sobre la verificación.
    /// </summary>
    /// <remarks>
    /// Es tolerante a la concurrencia: si otra solicitud canjeó el código mientras tanto,
    /// el contador simplemente no se actualiza y la operación no falla.
    /// </remarks>
    Task RegisterFailedAttemptAsync(
        UserEmailVerification verification,
        CancellationToken cancellationToken);
}
