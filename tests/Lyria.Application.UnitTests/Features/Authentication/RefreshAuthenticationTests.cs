using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Authentication;
using Lyria.Application.Features.Authentication.Refresh;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Authentication;

public sealed class RefreshAuthenticationTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 4, 23, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRefreshTokenRepository _sessions = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeAccessTokenService _accessTokens = new();
    private readonly FakeRefreshTokenGenerator _refreshTokens = new();
    private readonly FakeAuthenticationSessionWriter _writer = new();
    private readonly FixedTimeProvider _timeProvider = new(UtcNow);

    private RefreshAuthenticationCommandHandler CreateHandler() =>
        new(_sessions, _users, _accessTokens, _refreshTokens, _writer,
            _timeProvider, NullLogger<RefreshAuthenticationCommandHandler>.Instance);

    /// <summary>
    /// Siembra un usuario en el estado indicado. El estado predeterminado es
    /// <see cref="UserStatus.Active"/>: desde que existe la verificación de correo, es el
    /// único con el que se puede renovar una sesión.
    /// </summary>
    private User SeedUser(UserStatus status = UserStatus.Active)
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", $"{Guid.NewGuid():N}@email.com",
            "hash", null, null, null);

        if (status != UserStatus.Unverified)
        {
            user.ChangeStatus(UserStatus.Active);
        }

        if (status is UserStatus.Suspended or UserStatus.Deleted)
        {
            user.ChangeStatus(UserStatus.Suspended);
        }

        if (status == UserStatus.Deleted)
        {
            user.ChangeStatus(UserStatus.Deleted);
        }

        _users.Seed(user);

        return user;
    }

    /// <summary>
    /// Crea una sesión y devuelve el refresh token en claro correspondiente.
    /// </summary>
    private string SeedSession(
        UserId userId,
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null,
        bool revoked = false)
    {
        GeneratedRefreshTokenPair pair = NextToken();

        DateTime created = createdAtUtc ?? UtcNow.UtcDateTime.AddDays(-1);
        DateTime expires = expiresAtUtc ?? UtcNow.UtcDateTime.AddDays(29);

        var session = UserRefreshToken.Create(
            UserRefreshTokenId.New(), userId, pair.Hash, created, expires);

        if (revoked)
        {
            session.Revoke(created.AddHours(1));
        }

        _sessions.Seed(session);

        return pair.Token;
    }

    private GeneratedRefreshTokenPair NextToken()
    {
        var generated = _refreshTokens.Generate();
        return new GeneratedRefreshTokenPair(generated.Value, generated.Hash);
    }

    private sealed record GeneratedRefreshTokenPair(string Token, string Hash);

    private async Task<Result<AuthenticationResponse>> RefreshAsync(string token) =>
        await CreateHandler().Handle(
            new RefreshAuthenticationCommand(token), TestContext.Current.CancellationToken);

    // --- Caso válido ---

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsANewTokenPair()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id);

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal($"access-token-{user.Id.Value}", result.Value.AccessToken);
        Assert.NotEqual(token, result.Value.RefreshToken);
        Assert.Equal(
            UtcNow.UtcDateTime.Add(_refreshTokens.Lifetime),
            result.Value.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Refresh_RevokesThePreviousSessionAndCreatesANewOne()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id);

        await RefreshAsync(token);

        Assert.True(_writer.RotationCommitted);

        Assert.NotNull(_writer.RevokedSession);
        Assert.True(_writer.RevokedSession.IsRevoked);
        Assert.Equal(UtcNow.UtcDateTime, _writer.RevokedSession.RevokedAtUtc);

        Assert.NotNull(_writer.CreatedSession);
        Assert.False(_writer.CreatedSession.IsRevoked);
        Assert.Equal(user.Id, _writer.CreatedSession.UserId);
        Assert.NotEqual(_writer.RevokedSession.TokenHash, _writer.CreatedSession.TokenHash);
    }

    [Fact]
    public async Task Refresh_PersistsOnlyTheHashOfTheNewToken()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id);

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.NotNull(_writer.CreatedSession);
        Assert.NotEqual(result.Value.RefreshToken, _writer.CreatedSession.TokenHash);
        Assert.Equal(
            _refreshTokens.ComputeHash(result.Value.RefreshToken),
            _writer.CreatedSession.TokenHash);
    }

    // --- Rechazos ---

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsInvalidRefreshToken()
    {
        Result<AuthenticationResponse> result = await RefreshAsync("token-inexistente");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsInvalidRefreshToken()
    {
        User user = SeedUser();
        string token = SeedSession(
            user.Id,
            createdAtUtc: UtcNow.UtcDateTime.AddDays(-31),
            expiresAtUtc: UtcNow.UtcDateTime.AddDays(-1));

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsInvalidRefreshToken()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id, revoked: true);

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", result.Error.Code);
    }

    /// <summary>
    /// Reutilizar un refresh token ya rotado debe fallar: la rotación lo dejó revocado.
    /// </summary>
    [Fact]
    public async Task Refresh_ReusingARotatedToken_ReturnsInvalidRefreshToken()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id);

        Result<AuthenticationResponse> first = await RefreshAsync(token);
        Assert.True(first.IsSuccess);

        Result<AuthenticationResponse> second = await RefreshAsync(token);

        Assert.True(second.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", second.Error.Code);
    }

    [Theory]
    [InlineData(UserStatus.Unverified)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deleted)]
    public async Task Refresh_WhenTheUserIsNoLongerAuthenticable_Fails(UserStatus status)
    {
        User user = SeedUser(status);
        string token = SeedSession(user.Id);

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", result.Error.Code);
        Assert.False(_writer.RotationCommitted);
    }

    [Fact]
    public async Task Refresh_WhenTheUserNoLongerExists_Fails()
    {
        // Sesión de un usuario que nunca se sembró en el repositorio.
        string token = SeedSession(UserId.New());

        Result<AuthenticationResponse> result = await RefreshAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidRefreshToken", result.Error.Code);
    }

    /// <summary>
    /// Todos los motivos de rechazo deben producir un error idéntico.
    /// </summary>
    [Fact]
    public async Task Refresh_AllRejections_ProduceAnIdenticalError()
    {
        Error unknown = (await RefreshAsync("token-inexistente")).Error;

        var expiredTest = new RefreshAuthenticationTests();
        User expiredUser = expiredTest.SeedUser();
        string expiredToken = expiredTest.SeedSession(
            expiredUser.Id,
            createdAtUtc: UtcNow.UtcDateTime.AddDays(-31),
            expiresAtUtc: UtcNow.UtcDateTime.AddDays(-1));
        Error expired = (await expiredTest.RefreshAsync(expiredToken)).Error;

        var revokedTest = new RefreshAuthenticationTests();
        User revokedUser = revokedTest.SeedUser();
        string revokedToken = revokedTest.SeedSession(revokedUser.Id, revoked: true);
        Error revoked = (await revokedTest.RefreshAsync(revokedToken)).Error;

        var suspendedTest = new RefreshAuthenticationTests();
        User suspendedUser = suspendedTest.SeedUser(UserStatus.Suspended);
        string suspendedToken = suspendedTest.SeedSession(suspendedUser.Id);
        Error suspended = (await suspendedTest.RefreshAsync(suspendedToken)).Error;

        Assert.Equal(unknown, expired);
        Assert.Equal(unknown, revoked);
        Assert.Equal(unknown, suspended);
    }

    // --- Atomicidad ---

    [Fact]
    public async Task Refresh_WhenTheRotationFails_TheErrorPropagatesAndNothingIsCommitted()
    {
        User user = SeedUser();
        string token = SeedSession(user.Id);

        _writer.FailureToThrow = new InvalidOperationException("fallo al crear la sesión");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => RefreshAsync(token));

        Assert.False(_writer.RotationCommitted);
    }
}
