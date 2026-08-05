using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Authentication.Refresh;

/// <summary>
/// Renovación del par de tokens aplicando rotación del refresh token.
/// </summary>
/// <param name="RefreshToken">
/// Refresh token opaco entregado en el inicio de sesión o en la renovación anterior.
/// </param>
public sealed record RefreshAuthenticationCommand(
    string RefreshToken) : ICommand<AuthenticationResponse>;
