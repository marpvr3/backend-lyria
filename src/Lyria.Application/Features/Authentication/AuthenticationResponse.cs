namespace Lyria.Application.Features.Authentication;

/// <summary>
/// Par de tokens emitido por el inicio de sesión o por la renovación.
/// </summary>
/// <param name="TokenType">Esquema de autorización. Siempre <c>Bearer</c>.</param>
/// <param name="AccessToken">Access token de corta duración.</param>
/// <param name="AccessTokenExpiresAtUtc">Instante UTC en que expira el access token.</param>
/// <param name="RefreshToken">
/// Refresh token opaco de larga duración. Se entrega una única vez: el servidor solo
/// conserva su hash y no puede volver a mostrarlo.
/// </param>
/// <param name="RefreshTokenExpiresAtUtc">Instante UTC en que expira el refresh token.</param>
/// <param name="User">Perfil mínimo del usuario autenticado.</param>
public sealed record AuthenticationResponse(
    string TokenType,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    AuthenticatedUserResponse User);

/// <summary>
/// Datos mínimos del usuario que acompañan a la autenticación.
/// </summary>
/// <param name="UserId">Identificador del usuario.</param>
/// <param name="Name">Nombre del usuario.</param>
/// <param name="LastName">Apellido del usuario.</param>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Status">Estado actual de la cuenta.</param>
public sealed record AuthenticatedUserResponse(
    Guid UserId,
    string Name,
    string LastName,
    string Email,
    string Status);
