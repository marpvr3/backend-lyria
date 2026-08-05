using Lyria.Domain.Users.RefreshTokens;

namespace Lyria.Application.Abstractions.Persistence;

/// <summary>
/// Acceso a las sesiones de refresh token de un usuario.
/// </summary>
public interface IUserRefreshTokenRepository
{
    /// <summary>
    /// Localiza la sesión cuyo hash coincide con el indicado.
    /// </summary>
    /// <remarks>
    /// La búsqueda se hace siempre por hash: el token en claro nunca llega a la
    /// base de datos ni aparece en una consulta.
    /// </remarks>
    Task<UserRefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);
}
