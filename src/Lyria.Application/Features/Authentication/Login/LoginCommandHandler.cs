using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.Authentication.Login;

/// <summary>
/// Verifica las credenciales y abre una sesión móvil.
/// </summary>
/// <remarks>
/// Todos los rechazos devuelven <see cref="AuthenticationErrors.InvalidCredentials"/>,
/// sin importar si el correo no existe, la contraseña es incorrecta o la cuenta está
/// suspendida o eliminada. Además, la verificación de contraseña se ejecuta también
/// cuando el usuario no existe, para que el tiempo de respuesta no delate la diferencia.
/// </remarks>
public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAuthenticationSessionWriter sessionWriter,
    TimeProvider timeProvider,
    ILogger<LoginCommandHandler> logger)
    : ICommandHandler<LoginCommand, AuthenticationResponse>
{
    public async ValueTask<Result<AuthenticationResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Normalización con las mismas reglas que usa el registro.
        string normalizedEmail = User.NormalizeEmail(command.Email);

        // 2. Búsqueda del usuario.
        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // 3. Verificación de contraseña. Si el usuario no existe se verifica contra un
        //    hash señuelo para conservar un coste computacional equivalente.
        PasswordVerificationOutcome outcome = passwordHasher.Verify(
            user?.PasswordHash ?? passwordHasher.NonMatchingHash,
            command.Password);

        if (outcome == PasswordVerificationOutcome.Failed || user is null)
        {
            // Mensaje genérico y sin el correo: los intentos fallidos no deben permitir
            // reconstruir qué cuentas existen a partir de los logs.
            AuthenticationLog.LoginFailed(logger);

            return Result.Failure<AuthenticationResponse>(
                AuthenticationErrors.InvalidCredentials());
        }

        // 4. Estado de la cuenta. El rechazo es indistinguible de una credencial inválida.
        if (!AuthenticationPolicy.AllowsAuthentication(user.Status))
        {
            AuthenticationLog.LoginFailed(logger);

            return Result.Failure<AuthenticationResponse>(
                AuthenticationErrors.InvalidCredentials());
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // 5. Rehash cuando el hash almacenado quedó obsoleto.
        if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(command.Password));

            AuthenticationLog.PasswordRehashed(logger, user.Id.Value);
        }

        // 6. Última conexión.
        user.RegisterLastLogin(utcNow);

        // 7. Refresh token opaco. Solo se persiste su hash.
        GeneratedRefreshToken refreshToken = refreshTokenGenerator.Generate();
        DateTime refreshTokenExpiresAtUtc = utcNow.Add(refreshTokenGenerator.Lifetime);

        var session = UserRefreshToken.Create(
            UserRefreshTokenId.New(),
            user.Id,
            refreshToken.Hash,
            utcNow,
            refreshTokenExpiresAtUtc);

        // 8. Confirmación atómica de última conexión, posible rehash y sesión.
        await sessionWriter.CompleteLoginAsync(user, session, cancellationToken);

        // 9. Access token de corta duración.
        AccessToken accessToken = accessTokenService.Issue(user.Id);

        AuthenticationLog.LoginSucceeded(logger, user.Id.Value);

        return Result.Success(new AuthenticationResponse(
            AuthenticationPolicy.TokenType,
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshTokenExpiresAtUtc,
            new AuthenticatedUserResponse(
                user.Id.Value,
                user.Name,
                user.LastName,
                user.Email,
                user.Status.ToString())));
    }
}
