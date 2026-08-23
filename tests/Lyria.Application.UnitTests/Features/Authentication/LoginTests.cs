using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Authentication;
using Lyria.Application.Features.Authentication.Login;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Authentication;

public sealed class LoginTests
{
    private const string Email = "andres@email.com";
    private const string Password = "Password123";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 4, 23, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeAccessTokenService _accessTokens = new();
    private readonly FakeRefreshTokenGenerator _refreshTokens = new();
    private readonly FakeAuthenticationSessionWriter _writer = new();
    private readonly FixedTimeProvider _timeProvider = new(UtcNow);

    private LoginCommandHandler CreateHandler() =>
        new(_users, _passwordHasher, _accessTokens, _refreshTokens, _writer,
            _timeProvider, NullLogger<LoginCommandHandler>.Instance);

    /// <summary>
    /// Siembra un usuario en el estado indicado. El estado predeterminado es
    /// <see cref="UserStatus.Active"/>: desde que existe la verificación de correo, es el
    /// único con el que se puede iniciar sesión.
    /// </summary>
    private User SeedUser(
        UserStatus status = UserStatus.Active,
        string password = Password,
        string email = Email)
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", email,
            _passwordHasher.Hash(password), "3001234567",
            new DateOnly(1978, 12, 25), null);

        MoveToStatus(user, status);

        _users.Seed(user);

        return user;
    }

    /// <summary>
    /// Recorre las transiciones válidas del agregado hasta el estado buscado.
    /// </summary>
    private static void MoveToStatus(User user, UserStatus status)
    {
        if (status == UserStatus.Unverified)
        {
            return;
        }

        user.ChangeStatus(UserStatus.Active);

        if (status == UserStatus.Active)
        {
            return;
        }

        user.ChangeStatus(UserStatus.Suspended);

        if (status == UserStatus.Suspended)
        {
            return;
        }

        user.ChangeStatus(UserStatus.Deleted);
    }

    private async Task<Result<AuthenticationResponse>> LoginAsync(
        string email = Email, string password = Password) =>
        await CreateHandler().Handle(
            new LoginCommand(email, password), TestContext.Current.CancellationToken);

    // --- Rechazos ---

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        Result<AuthenticationResponse> result = await LoginAsync("nadie@email.com");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        SeedUser();

        Result<AuthenticationResponse> result = await LoginAsync(password: "OtraClave999");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
    }

    [Theory]
    [InlineData(UserStatus.Unverified)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deleted)]
    public async Task Login_WithNonAuthenticableStatus_ReturnsInvalidCredentials(
        UserStatus status)
    {
        SeedUser(status);

        Result<AuthenticationResponse> result = await LoginAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
    }

    /// <summary>
    /// Los cinco motivos de rechazo deben producir un error idéntico: ni el código, ni
    /// el mensaje, ni el tipo pueden permitir deducir si la cuenta existe o su estado.
    /// </summary>
    [Fact]
    public async Task Login_AllRejections_ProduceAnIdenticalError()
    {
        Error unknownEmail = (await LoginAsync("nadie@email.com")).Error;

        var wrongPasswordTest = new LoginTests();
        wrongPasswordTest.SeedUser();
        Error wrongPassword = (await wrongPasswordTest.LoginAsync(password: "Otra999")).Error;

        var unverifiedTest = new LoginTests();
        unverifiedTest.SeedUser(UserStatus.Unverified);
        Error unverified = (await unverifiedTest.LoginAsync()).Error;

        var suspendedTest = new LoginTests();
        suspendedTest.SeedUser(UserStatus.Suspended);
        Error suspended = (await suspendedTest.LoginAsync()).Error;

        var deletedTest = new LoginTests();
        deletedTest.SeedUser(UserStatus.Deleted);
        Error deleted = (await deletedTest.LoginAsync()).Error;

        Assert.Equal(unknownEmail, wrongPassword);
        Assert.Equal(unknownEmail, unverified);
        Assert.Equal(unknownEmail, suspended);
        Assert.Equal(unknownEmail, deleted);
    }

    /// <summary>
    /// Cuando el usuario no existe también se ejecuta una verificación de contraseña,
    /// contra el hash señuelo, para no producir una diferencia de tiempo observable.
    /// </summary>
    [Fact]
    public async Task Login_WithUnknownEmail_StillVerifiesAgainstTheDecoyHash()
    {
        await LoginAsync("nadie@email.com");

        Assert.Single(_passwordHasher.VerifiedHashes);
        Assert.Equal(FakePasswordHasher.NonMatchingHashValue, _passwordHasher.VerifiedHashes[0]);
    }

    [Fact]
    public async Task Login_WhenRejected_DoesNotCommitAnything()
    {
        SeedUser(UserStatus.Suspended);

        await LoginAsync();

        Assert.False(_writer.LoginCommitted);
    }

    // --- Estados permitidos ---

    [Fact]
    public async Task Login_WithActiveStatus_Succeeds()
    {
        SeedUser(UserStatus.Active);

        Result<AuthenticationResponse> result = await LoginAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(UserStatus.Active), result.Value.User.Status);
    }

    /// <summary>
    /// Una cuenta pendiente de verificar el correo no puede iniciar sesión, y el rechazo
    /// es indistinguible de una contraseña incorrecta o de un correo inexistente: un
    /// mensaje del tipo "debe verificar su correo" revelaría que la cuenta existe.
    /// </summary>
    [Fact]
    public async Task Login_WithUnverifiedStatus_IsIndistinguishableFromOtherRejections()
    {
        SeedUser(UserStatus.Unverified);

        Error unverified = (await LoginAsync()).Error;

        var unknownEmailTest = new LoginTests();
        Error unknownEmail = (await unknownEmailTest.LoginAsync("nadie@email.com")).Error;

        var wrongPasswordTest = new LoginTests();
        wrongPasswordTest.SeedUser();
        Error wrongPassword = (await wrongPasswordTest.LoginAsync(password: "Otra999")).Error;

        Assert.Equal(unknownEmail, unverified);
        Assert.Equal(wrongPassword, unverified);
        Assert.False(_writer.LoginCommitted);
    }

    [Fact]
    public async Task Login_DoesNotChangeTheUserStatus()
    {
        User user = SeedUser();

        await LoginAsync();

        Assert.Equal(UserStatus.Active, user.Status);
    }

    // --- Respuesta ---

    [Fact]
    public async Task Login_ReturnsBothTokensAndTheMinimumProfile()
    {
        User user = SeedUser();

        Result<AuthenticationResponse> result = await LoginAsync();

        Assert.True(result.IsSuccess);
        AuthenticationResponse response = result.Value;

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal($"access-token-{user.Id.Value}", response.AccessToken);
        Assert.Equal(_accessTokens.ExpiresAtUtc, response.AccessTokenExpiresAtUtc);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(
            UtcNow.UtcDateTime.Add(_refreshTokens.Lifetime),
            response.RefreshTokenExpiresAtUtc);

        Assert.Equal(user.Id.Value, response.User.UserId);
        Assert.Equal("Andres", response.User.Name);
        Assert.Equal("Perez", response.User.LastName);
        Assert.Equal(Email, response.User.Email);
    }

    [Fact]
    public async Task Login_NeverExposesThePasswordHash()
    {
        SeedUser();

        Result<AuthenticationResponse> result = await LoginAsync();

        string serialized = System.Text.Json.JsonSerializer.Serialize(result.Value);

        Assert.DoesNotContain("hashed_", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, serialized, StringComparison.Ordinal);
    }

    // --- Sesión persistida ---

    [Fact]
    public async Task Login_PersistsOnlyTheTokenHash_NeverThePlainToken()
    {
        SeedUser();

        Result<AuthenticationResponse> result = await LoginAsync();

        Assert.NotNull(_writer.CommittedSession);

        string plainToken = result.Value.RefreshToken;

        Assert.NotEqual(plainToken, _writer.CommittedSession.TokenHash);
        Assert.Equal(
            _refreshTokens.ComputeHash(plainToken),
            _writer.CommittedSession.TokenHash);
    }

    [Fact]
    public async Task Login_CommitsSessionAndUserTogether()
    {
        User user = SeedUser();

        await LoginAsync();

        Assert.True(_writer.LoginCommitted);
        Assert.Same(user, _writer.CommittedUser);
        Assert.NotNull(_writer.CommittedSession);
        Assert.Equal(user.Id, _writer.CommittedSession.UserId);
    }

    [Fact]
    public async Task Login_WhenTheCommitFails_ThePropagatedErrorIsNotSwallowed()
    {
        SeedUser();
        _writer.FailureToThrow = new InvalidOperationException("fallo de base de datos");

        await Assert.ThrowsAsync<InvalidOperationException>(() => LoginAsync());

        Assert.False(_writer.LoginCommitted);
    }

    // --- TimeProvider ---

    [Fact]
    public async Task Login_UsesTimeProviderForLastLoginAndSessionDates()
    {
        User user = SeedUser();

        await LoginAsync();

        Assert.Equal(UtcNow.UtcDateTime, user.LastLoginAtUtc);

        Assert.NotNull(_writer.CommittedSession);
        Assert.Equal(UtcNow.UtcDateTime, _writer.CommittedSession.CreatedAtUtc);
        Assert.Equal(
            UtcNow.UtcDateTime.Add(_refreshTokens.Lifetime),
            _writer.CommittedSession.ExpiresAtUtc);
    }

    // --- Rehash ---

    [Fact]
    public async Task Login_WithRehashNeeded_UpdatesThePasswordHash()
    {
        // El usuario se creó con el algoritmo antiguo (sin sufijo).
        User user = SeedUser();
        string legacyHash = user.PasswordHash;

        // El algoritmo vigente produce un hash distinto para la misma contraseña.
        _passwordHasher.HashSuffix = "_v2";
        _passwordHasher.SuccessOutcome = PasswordVerificationOutcome.SuccessRehashNeeded;

        Result<AuthenticationResponse> result = await LoginAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEqual(legacyHash, user.PasswordHash);
        Assert.EndsWith("_v2", user.PasswordHash, StringComparison.Ordinal);

        // El hash regenerado se confirma en la misma operación que el resto del login.
        Assert.True(_writer.LoginCommitted);
        Assert.Same(user, _writer.CommittedUser);
    }

    [Fact]
    public async Task Login_WithoutRehashNeeded_KeepsThePasswordHash()
    {
        User user = SeedUser();
        string originalHash = user.PasswordHash;

        _passwordHasher.HashSuffix = "_v2";

        await LoginAsync();

        Assert.Equal(originalHash, user.PasswordHash);
    }

    // --- Normalización ---

    [Fact]
    public async Task Login_NormalizesTheEmailBeforeLookup()
    {
        SeedUser();

        Result<AuthenticationResponse> result = await LoginAsync("  ANDRES@EMAIL.COM  ");

        Assert.True(result.IsSuccess);
    }
}
