using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.Extensions.Logging;

namespace Lyria.Application.Features.Authentication.Logout;

/// <summary>
/// Revoca la sesión asociada a un refresh token.
/// </summary>
/// <remarks>
/// La operación es idempotente y siempre tiene éxito: un token inexistente o ya
/// revocado produce el mismo resultado que una revocación efectiva, de modo que la
/// respuesta no revela si el token existía. La revocación es lógica; la fila se
/// conserva como historial de sesión.
/// </remarks>
public sealed class LogoutCommandHandler(
    IUserRefreshTokenRepository sessionRepository,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAuthenticationSessionWriter sessionWriter,
    TimeProvider timeProvider,
    ILogger<LogoutCommandHandler> logger)
    : ICommandHandler<LogoutCommand>
{
    public async ValueTask<Result> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tokenHash = refreshTokenGenerator.ComputeHash(command.RefreshToken);

        UserRefreshToken? session = await sessionRepository.GetByTokenHashAsync(
            tokenHash, cancellationToken);

        // Token inexistente o ya revocado: nada que hacer, y el resultado es el mismo
        // que el de una revocación efectiva.
        if (session is null || session.IsRevoked)
        {
            return Result.Success();
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        session.Revoke(utcNow);

        await sessionWriter.RevokeAsync(session, cancellationToken);

        AuthenticationLog.LogoutSucceeded(logger, session.UserId.Value);

        return Result.Success();
    }
}
