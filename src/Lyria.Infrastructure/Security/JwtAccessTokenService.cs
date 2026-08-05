using System.Globalization;
using System.Text;
using Lyria.Application.Abstractions.Security;
using Lyria.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Emite el access token como JWT firmado con HMAC-SHA256.
/// </summary>
/// <remarks>
/// El token incluye únicamente los claims necesarios para identificar la sesión:
/// <c>sub</c>, <c>jti</c>, <c>iat</c>, <c>nbf</c>, <c>exp</c>, <c>iss</c> y <c>aud</c>.
/// No transporta correo, teléfono, fecha de nacimiento, foto, roles, hash de contraseña
/// ni el refresh token.
/// </remarks>
internal sealed class JwtAccessTokenService(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider)
    : IAccessTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Issue(UserId userId)
    {
        // Se trunca a segundos porque los claims temporales de un JWT tienen precisión
        // de segundo: así la expiración anunciada al cliente coincide exactamente con
        // el claim exp del token.
        DateTime issuedAtUtc = TruncateToSeconds(timeProvider.GetUtcNow().UtcDateTime);
        DateTime expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenMinutes);

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAtUtc,
            NotBefore = issuedAtUtc,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] =
                    userId.Value.ToString(),
                [JwtRegisteredClaimNames.Jti] =
                    Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)
            }
        };

        string token = _tokenHandler.CreateToken(descriptor);

        return new AccessToken(token, expiresAtUtc);
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(
            value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond),
            value.Kind);
}
