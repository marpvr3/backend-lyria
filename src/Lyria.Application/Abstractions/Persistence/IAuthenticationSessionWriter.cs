using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;

namespace Lyria.Application.Abstractions.Persistence;

/// <summary>
/// Coordinador de persistencia de la autenticación móvil. Confirma en una única
/// transacción las escrituras que deben quedar consistentes entre sí.
/// </summary>
/// <remarks>
/// No es un Unit of Work genérico: expone exactamente las tres operaciones de este
/// flujo y no cede control transaccional al resto de la aplicación. La transacción
/// vive completamente en Infrastructure, siguiendo el mismo patrón que
/// <see cref="IMobileRegistrationWriter"/>.
/// </remarks>
public interface IAuthenticationSessionWriter
{
    /// <summary>
    /// Confirma un inicio de sesión: la última conexión del usuario, el posible
    /// rehash de su contraseña y la creación de la sesión.
    /// </summary>
    /// <remarks>
    /// Si la creación de la sesión falla, la actualización del usuario tampoco queda
    /// confirmada.
    /// </remarks>
    Task CompleteLoginAsync(
        User user,
        UserRefreshToken session,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirma la rotación de un refresh token: revocación de la sesión anterior y
    /// creación de la nueva.
    /// </summary>
    /// <remarks>
    /// Si la creación de la sesión nueva falla, la revocación de la anterior se
    /// revierte y el token previo conserva su estado original.
    /// </remarks>
    Task RotateAsync(
        UserRefreshToken revokedSession,
        UserRefreshToken createdSession,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirma la revocación lógica de una sesión durante el cierre de sesión.
    /// </summary>
    /// <remarks>
    /// La fila se conserva como historial: solo se establece su fecha de revocación.
    /// </remarks>
    Task RevokeAsync(
        UserRefreshToken revokedSession,
        CancellationToken cancellationToken);
}
