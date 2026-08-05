using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Emisor del access token de corta duración.
/// </summary>
/// <remarks>
/// Application desconoce el formato concreto del token: no referencia JWT, ni la clave
/// de firma, ni ninguna biblioteca criptográfica. Toda esa decisión vive en Infrastructure.
/// </remarks>
public interface IAccessTokenService
{
    /// <summary>
    /// Emite un access token para el usuario indicado.
    /// </summary>
    /// <param name="userId">Identidad que quedará asociada al token.</param>
    /// <returns>El token y el instante UTC exacto en que expira.</returns>
    AccessToken Issue(UserId userId);
}

/// <summary>
/// Access token emitido junto con su expiración efectiva.
/// </summary>
/// <param name="Value">Token que el cliente enviará como <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAtUtc">Instante UTC en que el token deja de ser válido.</param>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
