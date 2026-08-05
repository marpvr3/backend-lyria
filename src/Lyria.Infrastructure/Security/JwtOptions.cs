using System.ComponentModel.DataAnnotations;

namespace Lyria.Infrastructure.Security;

/// <summary>
/// Configuración del access token JWT. Se configura bajo la sección "Jwt".
/// </summary>
/// <remarks>
/// Los valores no sensibles pueden versionarse en appsettings.json. La clave de firma
/// <b>no</b>: debe suministrarse exclusivamente por variable de entorno
/// <c>Jwt__SigningKey</c> y nunca aparecer en el repositorio, en logs ni en Swagger.
///
/// La validación se ejecuta al arrancar (<c>ValidateOnStart</c>): una configuración
/// incompleta impide el inicio de la API en lugar de emitir tokens inseguros.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Longitud mínima exigida a la clave de firma.
    /// </summary>
    /// <remarks>
    /// HMAC-SHA256 requiere una clave de al menos 256 bits. Se exigen 32 caracteres
    /// como mínimo absoluto para no firmar con una clave más corta que el propio digest.
    /// </remarks>
    public const int SigningKeyMinLength = 32;

    /// <summary>
    /// Emisor del token.
    /// </summary>
    [Required(ErrorMessage = "El emisor (Jwt:Issuer) es obligatorio.")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiencia del token.
    /// </summary>
    [Required(ErrorMessage = "La audiencia (Jwt:Audience) es obligatoria.")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clave simétrica de firma. Solo por variable de entorno.
    /// </summary>
    [Required(ErrorMessage =
        "La clave de firma (Jwt:SigningKey) es obligatoria. " +
        "Configure la variable de entorno Jwt__SigningKey con un valor criptográficamente fuerte.")]
    [MinLength(SigningKeyMinLength, ErrorMessage =
        "La clave de firma (Jwt:SigningKey) debe tener al menos 32 caracteres.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Vigencia del access token, en minutos.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La vigencia del access token (Jwt:AccessTokenMinutes) debe ser mayor que cero.")]
    public int AccessTokenMinutes { get; set; }

    /// <summary>
    /// Vigencia del refresh token, en días.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage =
        "La vigencia del refresh token (Jwt:RefreshTokenDays) debe ser mayor que cero.")]
    public int RefreshTokenDays { get; set; }
}
