using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.Authentication.Login;

/// <summary>
/// Inicio de sesión con correo y contraseña desde la aplicación móvil.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta.</param>
/// <param name="Password">
/// Contraseña en claro. Solo se usa para verificarla contra el hash almacenado;
/// nunca se persiste ni se registra en logs.
/// </param>
public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthenticationResponse>;
