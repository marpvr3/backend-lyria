using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Authentication.Logout;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Authentication;

public sealed class LogoutTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 4, 23, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRefreshTokenRepository _sessions = new();
    private readonly FakeRefreshTokenGenerator _refreshTokens = new();
    private readonly FakeAuthenticationSessionWriter _writer = new();
    private readonly FixedTimeProvider _timeProvider = new(UtcNow);

    private LogoutCommandHandler CreateHandler() =>
        new(_sessions, _refreshTokens, _writer, _timeProvider,
            NullLogger<LogoutCommandHandler>.Instance);

    private (string Token, UserRefreshToken Session) SeedSession(bool revoked = false)
    {
        GeneratedRefreshToken generated = _refreshTokens.Generate();

        DateTime created = UtcNow.UtcDateTime.AddDays(-1);

        var session = UserRefreshToken.Create(
            UserRefreshTokenId.New(),
            UserId.New(),
            generated.Hash,
            created,
            UtcNow.UtcDateTime.AddDays(29));

        if (revoked)
        {
            session.Revoke(created.AddHours(1));
        }

        _sessions.Seed(session);

        return (generated.Value, session);
    }

    private async Task<Result> LogoutAsync(string token) =>
        await CreateHandler().Handle(
            new LogoutCommand(token), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Logout_WithValidToken_RevokesTheSession()
    {
        (string token, UserRefreshToken session) = SeedSession();

        Result result = await LogoutAsync(token);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsRevoked);
        Assert.Equal(UtcNow.UtcDateTime, session.RevokedAtUtc);
        Assert.True(_writer.RevocationCommitted);
    }

    [Fact]
    public async Task Logout_KeepsTheSessionHistory()
    {
        (string token, UserRefreshToken session) = SeedSession();
        string originalHash = session.TokenHash;

        await LogoutAsync(token);

        // La fila se conserva: no hay borrado físico.
        Assert.Single(_sessions.Sessions);
        Assert.Equal(originalHash, _sessions.Sessions[0].TokenHash);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_Succeeds()
    {
        Result result = await LogoutAsync("token-inexistente");

        Assert.True(result.IsSuccess);
        Assert.False(_writer.RevocationCommitted);
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_Succeeds()
    {
        (string token, UserRefreshToken session) = SeedSession(revoked: true);
        DateTime? originalRevocation = session.RevokedAtUtc;

        Result result = await LogoutAsync(token);

        Assert.True(result.IsSuccess);

        // No se vuelve a revocar ni se altera la fecha original.
        Assert.Equal(originalRevocation, session.RevokedAtUtc);
        Assert.False(_writer.RevocationCommitted);
    }

    [Fact]
    public async Task Logout_IsIdempotent()
    {
        (string token, UserRefreshToken session) = SeedSession();

        Result first = await LogoutAsync(token);
        DateTime? revokedAfterFirst = session.RevokedAtUtc;

        Result second = await LogoutAsync(token);
        Result third = await LogoutAsync(token);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsSuccess);
        Assert.Equal(revokedAfterFirst, session.RevokedAtUtc);
    }

    [Fact]
    public async Task Logout_LooksUpByHash_NeverByThePlainToken()
    {
        (string token, UserRefreshToken session) = SeedSession();

        await LogoutAsync(token);

        Assert.NotEqual(token, session.TokenHash);
        Assert.Equal(_refreshTokens.ComputeHash(token), session.TokenHash);
    }
}
