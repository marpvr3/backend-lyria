using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.Authentication;

/// <summary>
/// Mensajes de registro del flujo de autenticación, generados con
/// <see cref="LoggerMessageAttribute"/>.
/// </summary>
/// <remarks>
/// Ningún mensaje admite datos sensibles: no se registran correo, contraseña, hash de
/// contraseña, access token, refresh token, su hash ni la clave de firma. Los intentos
/// fallidos son deliberadamente genéricos para que los logs no permitan reconstruir qué
/// cuentas existen.
/// </remarks>
internal static partial class AuthenticationLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Warning,
        Message = "Intento de inicio de sesión fallido.")]
    public static partial void LoginFailed(ILogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Inicio de sesión exitoso del usuario {UserId}.")]
    public static partial void LoginSucceeded(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Se actualizó el hash de contraseña del usuario {UserId} al algoritmo vigente.")]
    public static partial void PasswordRehashed(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Intento de renovación con un refresh token no válido.")]
    public static partial void RefreshFailed(ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Renovación de sesión exitosa del usuario {UserId}.")]
    public static partial void RefreshSucceeded(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Cierre de sesión exitoso del usuario {UserId}.")]
    public static partial void LogoutSucceeded(ILogger logger, Guid userId);
}
