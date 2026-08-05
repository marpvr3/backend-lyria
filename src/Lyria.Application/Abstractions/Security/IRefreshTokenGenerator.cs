namespace Lyria.Application.Abstractions.Security;

/// <summary>
/// Generador de refresh tokens opacos de alta entropía.
/// </summary>
/// <remarks>
/// El token en claro solo existe en memoria durante la solicitud que lo emite y se
/// devuelve al cliente una única vez. Lo único que se persiste es su hash.
/// </remarks>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Genera un refresh token nuevo junto con el hash que debe almacenarse.
    /// </summary>
    GeneratedRefreshToken Generate();

    /// <summary>
    /// Calcula el hash de un refresh token recibido del cliente, para localizar
    /// la sesión correspondiente sin almacenar ni comparar el token en claro.
    /// </summary>
    string ComputeHash(string refreshToken);

    /// <summary>
    /// Duración configurada de un refresh token.
    /// </summary>
    TimeSpan Lifetime { get; }
}

/// <summary>
/// Par formado por un refresh token en claro y su hash almacenable.
/// </summary>
/// <param name="Value">
/// Token opaco que se entrega al cliente. Nunca debe persistirse ni registrarse en logs.
/// </param>
/// <param name="Hash">Hash SHA-256 hexadecimal que sí se persiste.</param>
public sealed record GeneratedRefreshToken(string Value, string Hash);
