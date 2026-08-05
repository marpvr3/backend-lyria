using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users.RefreshTokens;

/// <summary>
/// Sesión de larga duración de un usuario móvil. Representa un refresh token opaco
/// del que solo se conserva su hash SHA-256; el valor en texto plano se entrega una
/// única vez al cliente y nunca se persiste.
/// </summary>
/// <remarks>
/// La entidad no implementa <see cref="IAuditableEntity"/> de forma deliberada: ese
/// contrato exige una fecha de actualización que esta tabla no necesita ni almacena.
/// <see cref="CreatedAtUtc"/> lo asigna explícitamente el caso de uso a partir de
/// <see cref="TimeProvider"/>.
///
/// La revocación es lógica: <see cref="RevokedAtUtc"/> se establece y la fila se
/// conserva como historial. No existe borrado físico de sesiones.
/// </remarks>
public sealed class UserRefreshToken : Entity<UserRefreshTokenId>
{
    /// <summary>
    /// Longitud exacta de un hash SHA-256 representado en hexadecimal.
    /// </summary>
    public const int TokenHashLength = 64;

    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Indica si la sesión fue revocada explícitamente (logout o rotación).
    /// </summary>
    public bool IsRevoked => RevokedAtUtc is not null;

    private UserRefreshToken()
    {
    }

    private UserRefreshToken(
        UserRefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = null;
    }

    /// <summary>
    /// Crea una sesión a partir del hash del refresh token. Nunca recibe el token en claro.
    /// </summary>
    public static UserRefreshToken Create(
        UserRefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        ValidateUserId(userId);

        string normalizedHash = NormalizeTokenHash(tokenHash);
        ValidateTokenHash(normalizedHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new UserRefreshTokenException(
                "La fecha de expiración de la sesión debe ser posterior a su fecha de creación.");
        }

        return new UserRefreshToken(id, userId, normalizedHash, createdAtUtc, expiresAtUtc);
    }

    /// <summary>
    /// Indica si la sesión ya venció en el instante indicado.
    /// </summary>
    /// <remarks>
    /// Recibe el instante actual en lugar de leer el reloj del sistema para que el
    /// dominio permanezca determinista y las pruebas puedan usar <see cref="TimeProvider"/>.
    /// </remarks>
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    /// <summary>
    /// Indica si la sesión sigue siendo utilizable: ni revocada ni vencida.
    /// </summary>
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    /// <summary>
    /// Revoca la sesión de forma lógica. Una sesión ya revocada no puede revocarse de nuevo.
    /// </summary>
    public void Revoke(DateTime revokedAtUtc)
    {
        if (IsRevoked)
        {
            throw new UserRefreshTokenException(
                "La sesión ya se encuentra revocada.");
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new UserRefreshTokenException(
                "La fecha de revocación no puede ser anterior a la fecha de creación de la sesión.");
        }

        RevokedAtUtc = revokedAtUtc;
    }

    public static string NormalizeTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return string.Empty;
        }

        return tokenHash.Trim().ToLowerInvariant();
    }

    private static void ValidateUserId(UserId userId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new UserRefreshTokenException("El usuario de la sesión es obligatorio.");
        }
    }

    /// <summary>
    /// Exige un SHA-256 hexadecimal de 64 caracteres. Además de validar el formato,
    /// esta regla impide almacenar por error un refresh token en texto plano: los
    /// tokens opacos son Base64Url y no superan esta comprobación.
    /// </summary>
    private static void ValidateTokenHash(string tokenHash)
    {
        if (string.IsNullOrEmpty(tokenHash))
        {
            throw new UserRefreshTokenException("El hash del refresh token es obligatorio.");
        }

        if (tokenHash.Length != TokenHashLength)
        {
            throw new UserRefreshTokenException(
                $"El hash del refresh token debe tener exactamente {TokenHashLength} caracteres.");
        }

        foreach (char character in tokenHash)
        {
            bool isHexDigit = character is (>= '0' and <= '9') or (>= 'a' and <= 'f');

            if (!isHexDigit)
            {
                throw new UserRefreshTokenException(
                    "El hash del refresh token debe estar en formato hexadecimal.");
            }
        }
    }
}
