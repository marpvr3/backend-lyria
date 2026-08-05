using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.Authentication.Refresh;

/// <summary>
/// Renueva el par de tokens rotando el refresh token.
/// </summary>
/// <remarks>
/// La rotación revoca el token presentado y emite uno nuevo dentro de la misma
/// transacción, de modo que un refresh token nunca puede usarse dos veces. Un token
/// inexistente, vencido, revocado o reutilizado produce siempre
/// <see cref="AuthenticationErrors.InvalidRefreshToken"/>, sin distinguir la causa.
/// </remarks>
public sealed class RefreshAuthenticationCommandHandler(
    IUserRefreshTokenRepository sessionRepository,
    IUserRepository userRepository,
    IAccessTokenService accessTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAuthenticationSessionWriter sessionWriter,
    TimeProvider timeProvider,
    ILogger<RefreshAuthenticationCommandHandler> logger)
    : ICommandHandler<RefreshAuthenticationCommand, AuthenticationResponse>
{
    public async ValueTask<Result<AuthenticationResponse>> Handle(
        RefreshAuthenticationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 1. Hash del token recibido. El token en claro nunca llega a la base de datos.
        string tokenHash = refreshTokenGenerator.ComputeHash(command.RefreshToken);

        // 2-5. La sesión debe existir, no estar revocada y no haber vencido.
        UserRefreshToken? session = await sessionRepository.GetByTokenHashAsync(
            tokenHash, cancellationToken);

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (session is null || !session.IsActive(utcNow))
        {
            AuthenticationLog.RefreshFailed(logger);

            return Result.Failure<AuthenticationResponse>(
                AuthenticationErrors.InvalidRefreshToken());
        }

        // 6-7. El usuario debe seguir existiendo y conservar un estado que permita autenticarse.
        User? user = await userRepository.GetByIdAsync(session.UserId, cancellationToken);

        if (user is null || !AuthenticationPolicy.AllowsAuthentication(user.Status))
        {
            AuthenticationLog.RefreshFailed(logger);

            return Result.Failure<AuthenticationResponse>(
                AuthenticationErrors.InvalidRefreshToken());
        }

        // 8. Revocación del token presentado.
        session.Revoke(utcNow);

        // 9. Token nuevo.
        GeneratedRefreshToken refreshToken = refreshTokenGenerator.Generate();
        DateTime refreshTokenExpiresAtUtc = utcNow.Add(refreshTokenGenerator.Lifetime);

        var rotatedSession = UserRefreshToken.Create(
            UserRefreshTokenId.New(),
            user.Id,
            refreshToken.Hash,
            utcNow,
            refreshTokenExpiresAtUtc);

        // 10-11. Revocación y creación se confirman juntas o no se confirma ninguna.
        await sessionWriter.RotateAsync(session, rotatedSession, cancellationToken);

        // 12. Access token nuevo.
        AccessToken accessToken = accessTokenService.Issue(user.Id);

        AuthenticationLog.RefreshSucceeded(logger, user.Id.Value);

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
