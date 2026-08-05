using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Authentication.Logout;

/// <summary>
/// Cierre de sesión: revoca el refresh token indicado.
/// </summary>
/// <param name="RefreshToken">Refresh token opaco de la sesión a cerrar.</param>
public sealed record LogoutCommand(string RefreshToken) : ICommand;
