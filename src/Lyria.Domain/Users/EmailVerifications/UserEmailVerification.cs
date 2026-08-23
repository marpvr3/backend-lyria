using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users.EmailVerifications;

/// <summary>
/// Código de un solo uso con el que un usuario confirma que el correo con el que se
/// registró le pertenece. Solo se conserva el hash del código; el valor de seis dígitos
/// se entrega una única vez por correo electrónico y nunca se persiste.
/// </summary>
/// <remarks>
/// La entidad no implementa <see cref="IAuditableEntity"/> de forma deliberada: ese
/// contrato exige una fecha de actualización que la tabla autorizada no contiene.
/// <see cref="CreatedAtUtc"/> lo asigna explícitamente el caso de uso a partir de
/// <see cref="TimeProvider"/>.
///
/// Un usuario puede acumular varias verificaciones históricas —una por reenvío—, por lo
/// que la relación con <see cref="User"/> es de uno a muchos. La invalidación es lógica:
/// <see cref="UsedAtUtc"/> y <see cref="RevokedAtUtc"/> se establecen y la fila se
/// conserva como historial. No existe borrado físico.
///
/// La entidad no almacena el código en claro, el correo, la contraseña, tokens ni datos
/// del proveedor SMTP.
/// </remarks>
public sealed class UserEmailVerification : Entity<UserEmailVerificationId>
{
    /// <summary>
    /// Longitud exacta de un HMAC-SHA256 representado en hexadecimal.
    /// </summary>
    public const int CodeHashLength = 64;

    /// <summary>
    /// Número de intentos fallidos que invalidan un código cuando la configuración no
    /// indica otro valor. Es la regla centralizada del dominio.
    /// </summary>
    public const int DefaultMaximumFailedAttempts = 5;

    public UserId UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }

    /// <summary>
    /// Indica si el código ya se canjeó. Un código usado no puede volver a usarse.
    /// </summary>
    public bool IsUsed => UsedAtUtc is not null;

    /// <summary>
    /// Indica si el código se invalidó explícitamente, por un reenvío posterior o por
    /// agotar los intentos permitidos.
    /// </summary>
    public bool IsRevoked => RevokedAtUtc is not null;

    private UserEmailVerification()
    {
    }

    private UserEmailVerification(
        UserEmailVerificationId id,
        UserId userId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        UsedAtUtc = null;
        RevokedAtUtc = null;
        FailedAttempts = 0;
    }

    /// <summary>
    /// Crea una verificación a partir del hash del código. Nunca recibe el código en claro.
    /// </summary>
    public static UserEmailVerification Create(
        UserEmailVerificationId id,
        UserId userId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        ValidateUserId(userId);

        string normalizedHash = NormalizeCodeHash(codeHash);
        ValidateCodeHash(normalizedHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new UserEmailVerificationException(
                "La fecha de expiración del código debe ser posterior a su fecha de creación.");
        }

        return new UserEmailVerification(id, userId, normalizedHash, createdAtUtc, expiresAtUtc);
    }

    /// <summary>
    /// Indica si el código ya venció en el instante indicado.
    /// </summary>
    /// <remarks>
    /// Recibe el instante actual en lugar de leer el reloj del sistema para que el
    /// dominio permanezca determinista y las pruebas puedan usar <see cref="TimeProvider"/>.
    /// </remarks>
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    /// <summary>
    /// Indica si el código agotó los intentos fallidos permitidos.
    /// </summary>
    /// <param name="maximumFailedAttempts">
    /// Máximo configurado. Debe ser mayor que cero.
    /// </param>
    public bool HasExceededMaximumAttempts(
        int maximumFailedAttempts = DefaultMaximumFailedAttempts)
    {
        ValidateMaximumFailedAttempts(maximumFailedAttempts);

        return FailedAttempts >= maximumFailedAttempts;
    }

    /// <summary>
    /// Indica si el código sigue siendo canjeable: ni usado, ni revocado, ni vencido, ni
    /// con los intentos agotados.
    /// </summary>
    public bool IsActive(
        DateTime utcNow,
        int maximumFailedAttempts = DefaultMaximumFailedAttempts) =>
        !IsUsed &&
        !IsRevoked &&
        !IsExpired(utcNow) &&
        !HasExceededMaximumAttempts(maximumFailedAttempts);

    /// <summary>
    /// Registra un intento fallido de confirmación.
    /// </summary>
    /// <remarks>
    /// Solo se invoca cuando el código presentado no coincide. Un código usado o revocado
    /// no acumula intentos: la aplicación devuelve el error genérico sin tocar el contador.
    /// </remarks>
    public void RegisterFailedAttempt()
    {
        if (IsUsed)
        {
            throw new UserEmailVerificationException(
                "No se pueden registrar intentos sobre un código ya utilizado.");
        }

        if (IsRevoked)
        {
            throw new UserEmailVerificationException(
                "No se pueden registrar intentos sobre un código revocado.");
        }

        FailedAttempts++;
    }

    /// <summary>
    /// Marca el código como canjeado. Es la única forma de consumirlo.
    /// </summary>
    public void MarkAsUsed(DateTime usedAtUtc)
    {
        if (IsUsed)
        {
            throw new UserEmailVerificationException(
                "El código de verificación ya fue utilizado.");
        }

        if (IsRevoked)
        {
            throw new UserEmailVerificationException(
                "No se puede utilizar un código revocado.");
        }

        if (IsExpired(usedAtUtc))
        {
            throw new UserEmailVerificationException(
                "No se puede utilizar un código vencido.");
        }

        if (usedAtUtc < CreatedAtUtc)
        {
            throw new UserEmailVerificationException(
                "La fecha de uso no puede ser anterior a la fecha de creación del código.");
        }

        UsedAtUtc = usedAtUtc;
    }

    /// <summary>
    /// Invalida el código de forma lógica. La fila se conserva como historial.
    /// </summary>
    public void Revoke(DateTime revokedAtUtc)
    {
        if (IsRevoked)
        {
            throw new UserEmailVerificationException(
                "El código de verificación ya se encuentra revocado.");
        }

        if (IsUsed)
        {
            throw new UserEmailVerificationException(
                "No se puede revocar un código ya utilizado.");
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new UserEmailVerificationException(
                "La fecha de revocación no puede ser anterior a la fecha de creación del código.");
        }

        RevokedAtUtc = revokedAtUtc;
    }

    public static string NormalizeCodeHash(string codeHash)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
        {
            return string.Empty;
        }

        return codeHash.Trim().ToLowerInvariant();
    }

    private static void ValidateUserId(UserId userId)
    {
        if (userId.Value == Guid.Empty)
        {
            throw new UserEmailVerificationException(
                "El usuario de la verificación de correo es obligatorio.");
        }
    }

    /// <summary>
    /// Exige un hash hexadecimal de 64 caracteres. Además de validar el formato, esta
    /// regla impide almacenar por error el código en claro: seis dígitos no superan
    /// esta comprobación bajo ninguna circunstancia.
    /// </summary>
    private static void ValidateCodeHash(string codeHash)
    {
        if (string.IsNullOrEmpty(codeHash))
        {
            throw new UserEmailVerificationException(
                "El hash del código de verificación es obligatorio.");
        }

        if (codeHash.Length != CodeHashLength)
        {
            throw new UserEmailVerificationException(
                $"El hash del código de verificación debe tener exactamente {CodeHashLength} caracteres.");
        }

        foreach (char character in codeHash)
        {
            bool isHexDigit = character is (>= '0' and <= '9') or (>= 'a' and <= 'f');

            if (!isHexDigit)
            {
                throw new UserEmailVerificationException(
                    "El hash del código de verificación debe estar en formato hexadecimal.");
            }
        }
    }

    private static void ValidateMaximumFailedAttempts(int maximumFailedAttempts)
    {
        if (maximumFailedAttempts <= 0)
        {
            throw new UserEmailVerificationException(
                "El máximo de intentos fallidos debe ser mayor que cero.");
        }
    }
}
