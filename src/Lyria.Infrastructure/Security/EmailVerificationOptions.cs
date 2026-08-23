using System.ComponentModel.DataAnnotations;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Configuración de la verificación de correo. Se configura bajo la sección
/// "EmailVerification".
/// </summary>
/// <remarks>
/// Los valores de política pueden versionarse en appsettings.json. El secreto
/// <see cref="CodeSecret"/> <b>no</b>: debe suministrarse exclusivamente por variable de
/// entorno <c>EmailVerification__CodeSecret</c> y nunca aparecer en el repositorio, en
/// logs ni en Swagger.
///
/// La validación se ejecuta al arrancar (<c>ValidateOnStart</c>): una configuración
/// incompleta impide el inicio de la API en lugar de emitir códigos con un secreto
/// ausente o demasiado corto.
///
/// El secreto es independiente de <see cref="JwtOptions.SigningKey"/>: reutilizar la
/// clave de firma haría que comprometer uno de los dos mecanismos comprometiera el otro.
/// </remarks>
public sealed class EmailVerificationOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "EmailVerification";

    /// <summary>
    /// Longitud mínima exigida al secreto.
    /// </summary>
    /// <remarks>
    /// HMAC-SHA256 trabaja con bloques de 256 bits; se exigen 32 caracteres como mínimo
    /// absoluto para no derivar la clave de un valor más corto que el propio digest.
    /// </remarks>
    public const int CodeSecretMinLength = 32;

    /// <summary>
    /// Única longitud de código admitida en esta versión.
    /// </summary>
    public const int SupportedCodeLength = 6;

    /// <summary>
    /// Secreto con el que se calcula el HMAC de los códigos. Solo por variable de entorno.
    /// </summary>
    [Required(ErrorMessage =
        "El secreto de verificación (EmailVerification:CodeSecret) es obligatorio. " +
        "Configure la variable de entorno EmailVerification__CodeSecret con un valor " +
        "criptográficamente fuerte, distinto de Jwt__SigningKey.")]
    [MinLength(CodeSecretMinLength, ErrorMessage =
        "El secreto de verificación (EmailVerification:CodeSecret) debe tener al menos 32 caracteres.")]
    public string CodeSecret { get; set; } = string.Empty;

    /// <summary>
    /// Número de dígitos del código. En esta versión debe ser exactamente 6.
    /// </summary>
    [Range(SupportedCodeLength, SupportedCodeLength, ErrorMessage =
        "La longitud del código (EmailVerification:CodeLength) debe ser exactamente 6 en esta versión.")]
    public int CodeLength { get; set; } = SupportedCodeLength;

    /// <summary>
    /// Vigencia del código, en minutos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La vigencia del código (EmailVerification:ExpirationMinutes) debe ser mayor que cero.")]
    public int ExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Intentos fallidos que invalidan un código.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "El máximo de intentos (EmailVerification:MaximumFailedAttempts) debe ser mayor que cero.")]
    public int MaximumFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Intervalo mínimo entre dos envíos al mismo usuario, en segundos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "El intervalo entre envíos (EmailVerification:ResendCooldownSeconds) debe ser mayor que cero.")]
    public int ResendCooldownSeconds { get; set; } = 60;
}
