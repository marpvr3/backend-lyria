using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Lyria.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Genera refresh tokens opacos y calcula el hash con el que se almacenan.
/// </summary>
/// <remarks>
/// El token son 32 bytes de un generador criptográficamente seguro
/// (<see cref="RandomNumberGenerator"/>), codificados en Base64Url para que puedan
/// viajar en JSON sin escapes. No es un JWT y no contiene información alguna.
///
/// Se almacena su SHA-256 en hexadecimal. Usar SHA-256 aquí es correcto —y no
/// contradice el uso de un password hasher lento— porque el token ya tiene 256 bits
/// de entropía: no existe un espacio de búsqueda que un atacante pueda recorrer, que
/// es justamente el riesgo del que protege un hasher con factor de trabajo.
/// </remarks>
internal sealed class RefreshTokenGenerator(IOptions<JwtOptions> options)
    : IRefreshTokenGenerator
{
    /// <summary>
    /// Entropía del token, en bytes.
    /// </summary>
    private const int TokenBytes = 32;

    private readonly JwtOptions _options = options.Value;

    public TimeSpan Lifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public GeneratedRefreshToken Generate()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(TokenBytes);

        string token = Base64Url.EncodeToString(tokenBytes);

        return new GeneratedRefreshToken(token, ComputeHash(token));
    }

    public string ComputeHash(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexStringLower(hash);
    }
}
