using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;

namespace Lyria.Application.Abstractions.Persistence;

/// <summary>
/// Acceso a las verificaciones de correo de un usuario.
/// </summary>
/// <remarks>
/// Las consultas se hacen siempre por usuario, nunca por hash del código: el cliente
/// envía el código en claro y solo el caso de uso, que ya identificó al usuario por su
/// correo, comprueba la correspondencia. Buscar por hash permitiría canjear un código
/// sin conocer la cuenta a la que pertenece.
/// </remarks>
public interface IUserEmailVerificationRepository
{
    /// <summary>
    /// Recupera la verificación vigente más reciente del usuario: la última que no está
    /// ni usada ni revocada. Puede estar vencida o con los intentos agotados; esa
    /// comprobación corresponde al caso de uso.
    /// </summary>
    Task<UserEmailVerification?> GetLatestPendingAsync(
        UserId userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Recupera todas las verificaciones del usuario que aún no están usadas ni revocadas.
    /// Es el conjunto que un reenvío debe invalidar.
    /// </summary>
    Task<IReadOnlyList<UserEmailVerification>> GetPendingAsync(
        UserId userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fecha de creación de la última verificación emitida al usuario, usada para aplicar
    /// el intervalo mínimo entre envíos. Devuelve <c>null</c> si nunca se emitió ninguna.
    /// </summary>
    Task<DateTime?> GetLastCreatedAtUtcAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
