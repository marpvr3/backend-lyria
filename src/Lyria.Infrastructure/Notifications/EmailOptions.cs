namespace Lyria.Infrastructure.Notifications;

/// <summary>
/// Configuración del proveedor de correo saliente. Se configura bajo la sección "Email".
/// </summary>
/// <remarks>
/// El binding no aplica validación de arranque de forma deliberada: un ambiente que no
/// envía correo —desarrollo local o el host de pruebas funcionales, que sustituye el
/// remitente— no debe impedir el inicio de toda la API. La configuración se comprueba al
/// enviar y un problema produce un error técnico registrado sin secretos, nunca una
/// respuesta con detalles SMTP.
///
/// <see cref="Username"/> y <see cref="Password"/> son secretos: se suministran
/// exclusivamente por variable de entorno (<c>Email__Username</c>, <c>Email__Password</c>)
/// y nunca se versionan ni se registran en logs.
/// </remarks>
public sealed class EmailOptions
{
    /// <summary>
    /// Nombre de la sección en la configuración.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Servidor SMTP.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// Puerto SMTP.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Indica si la conexión debe elevarse a TLS con STARTTLS.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Usuario de autenticación SMTP. Secreto.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña de autenticación SMTP. Secreto.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Dirección remitente de los mensajes.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Nombre visible del remitente.
    /// </summary>
    public string FromName { get; set; } = "Lyria";
}
