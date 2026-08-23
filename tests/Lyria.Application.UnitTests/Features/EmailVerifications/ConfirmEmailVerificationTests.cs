using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.EmailVerifications.Confirm;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EmailVerifications;

public sealed class ConfirmEmailVerificationTests
{
    private const string Email = "andres@email.com";
    private const string Code = "482731";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserEmailVerificationRepository _verifications = new();
    private readonly FakeEmailVerificationWriter _writer = new();
    private readonly FakeEmailVerificationCodeHasher _codeHasher = new();
    private readonly FakeEmailVerificationDefaults _defaults = new();
    private readonly FixedTimeProvider _timeProvider = new(UtcNow);

    private ConfirmEmailVerificationCommandHandler CreateHandler() =>
        new(_users, _verifications, _writer, _codeHasher, _defaults, _timeProvider,
            NullLogger<ConfirmEmailVerificationCommandHandler>.Instance);

    private User SeedUser(UserStatus status = UserStatus.Unverified)
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", Email, "hash", null, null, null);

        if (status != UserStatus.Unverified)
        {
            user.MarkEmailAsVerified();
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

    private UserEmailVerification SeedVerification(
        UserId userId,
        string code = Code,
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null)
    {
        DateTime created = createdAtUtc ?? UtcNow.UtcDateTime.AddMinutes(-1);

        var verification = UserEmailVerification.Create(
            UserEmailVerificationId.New(),
            userId,
            _codeHasher.ComputeHash(code),
            created,
            expiresAtUtc ?? created.AddMinutes(15));

        _verifications.Seed(verification);

        return verification;
    }

    private async Task<Result> ConfirmAsync(string email = Email, string code = Code) =>
        await CreateHandler().Handle(
            new ConfirmEmailVerificationCommand(email, code),
            TestContext.Current.CancellationToken);

    private static void AssertInvalidCode(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal("EmailVerification.InvalidCode", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(
            "El código de verificación no es válido o ha vencido.", result.Error.Description);
    }

    // --- Confirmación correcta ---

    [Fact]
    public async Task Confirm_WithAValidCode_Succeeds()
    {
        User user = SeedUser();
        SeedVerification(user.Id);

        Result result = await ConfirmAsync();

        Assert.True(result.IsSuccess);
        Assert.True(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_MarksTheEmailAsVerifiedAndActivatesTheAccount()
    {
        User user = SeedUser();
        SeedVerification(user.Id);

        await ConfirmAsync();

        Assert.True(user.IsEmailVerified);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public async Task Confirm_MarksTheCodeAsUsedWithTheTimeProviderInstant()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        await ConfirmAsync();

        Assert.True(verification.IsUsed);
        Assert.Equal(UtcNow.UtcDateTime, verification.UsedAtUtc);
    }

    /// <summary>
    /// El usuario y la verificación se confirman en la misma operación transaccional.
    /// </summary>
    [Fact]
    public async Task Confirm_CommitsTheUserAndTheVerificationTogether()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        await ConfirmAsync();

        Assert.Same(user, _writer.ConfirmedUser);
        Assert.Same(verification, _writer.ConfirmedVerification);
    }

    [Fact]
    public async Task Confirm_NormalizesTheEmailBeforeLookup()
    {
        User user = SeedUser();
        SeedVerification(user.Id);

        Result result = await ConfirmAsync("  ANDRES@EMAIL.COM  ");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Confirm_UsesTheMostRecentPendingVerification()
    {
        User user = SeedUser();
        SeedVerification(user.Id, "111111", UtcNow.UtcDateTime.AddMinutes(-10));
        UserEmailVerification latest = SeedVerification(
            user.Id, Code, UtcNow.UtcDateTime.AddMinutes(-1));

        Result result = await ConfirmAsync();

        Assert.True(result.IsSuccess);
        Assert.True(latest.IsUsed);
    }

    // --- Rechazos ---

    [Fact]
    public async Task Confirm_WithAnUnknownEmail_ReturnsInvalidCode()
    {
        AssertInvalidCode(await ConfirmAsync("nadie@email.com"));

        Assert.False(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_ReturnsInvalidCode()
    {
        User user = SeedUser();
        SeedVerification(user.Id);

        AssertInvalidCode(await ConfirmAsync(code: "000000"));

        Assert.False(_writer.ConfirmationCommitted);
        Assert.Equal(UserStatus.Unverified, user.Status);
        Assert.False(user.IsEmailVerified);
    }

    [Fact]
    public async Task Confirm_WithAnExpiredCode_ReturnsInvalidCode()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(
            user.Id,
            createdAtUtc: UtcNow.UtcDateTime.AddMinutes(-20),
            expiresAtUtc: UtcNow.UtcDateTime.AddMinutes(-5));

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);

        // Un código vencido no acumula intentos.
        Assert.Equal(0, verification.FailedAttempts);
        Assert.False(_writer.FailedAttemptCommitted);
    }

    [Fact]
    public async Task Confirm_WithAUsedCode_ReturnsInvalidCode()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);
        verification.MarkAsUsed(UtcNow.UtcDateTime.AddSeconds(-10));

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_WithARevokedCode_ReturnsInvalidCode()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);
        verification.Revoke(UtcNow.UtcDateTime.AddSeconds(-10));

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
        Assert.Equal(0, verification.FailedAttempts);
    }

    [Fact]
    public async Task Confirm_WithoutAnyVerification_ReturnsInvalidCode()
    {
        SeedUser();

        AssertInvalidCode(await ConfirmAsync());
    }

    [Fact]
    public async Task Confirm_ForAnAlreadyVerifiedUser_ReturnsInvalidCode()
    {
        User user = SeedUser(UserStatus.Active);
        SeedVerification(user.Id);

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
    }

    [Theory]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deleted)]
    public async Task Confirm_ForAUserInANonAllowedStatus_ReturnsInvalidCode(UserStatus status)
    {
        User user = SeedUser(status);
        SeedVerification(user.Id);

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
        Assert.Equal(status, user.Status);
    }

    /// <summary>
    /// Ninguno de los ocho rechazos puede distinguirse de los demás.
    /// </summary>
    [Fact]
    public async Task Confirm_AllRejections_ProduceAnIdenticalError()
    {
        var unknownEmail = new ConfirmEmailVerificationTests();

        var wrongCode = new ConfirmEmailVerificationTests();
        wrongCode.SeedVerification(wrongCode.SeedUser().Id);

        var expired = new ConfirmEmailVerificationTests();
        expired.SeedVerification(
            expired.SeedUser().Id,
            createdAtUtc: UtcNow.UtcDateTime.AddMinutes(-30),
            expiresAtUtc: UtcNow.UtcDateTime.AddMinutes(-10));

        var used = new ConfirmEmailVerificationTests();
        UserEmailVerification usedVerification =
            used.SeedVerification(used.SeedUser().Id);
        usedVerification.MarkAsUsed(UtcNow.UtcDateTime.AddSeconds(-5));

        var revoked = new ConfirmEmailVerificationTests();
        UserEmailVerification revokedVerification =
            revoked.SeedVerification(revoked.SeedUser().Id);
        revokedVerification.Revoke(UtcNow.UtcDateTime.AddSeconds(-5));

        var exhausted = new ConfirmEmailVerificationTests();
        UserEmailVerification exhaustedVerification =
            exhausted.SeedVerification(exhausted.SeedUser().Id);

        for (int attempt = 0; attempt < exhausted._defaults.MaximumFailedAttempts; attempt++)
        {
            exhaustedVerification.RegisterFailedAttempt();
        }

        var verified = new ConfirmEmailVerificationTests();
        verified.SeedVerification(verified.SeedUser(UserStatus.Active).Id);

        var suspended = new ConfirmEmailVerificationTests();
        suspended.SeedVerification(suspended.SeedUser(UserStatus.Suspended).Id);

        Error[] errors =
        [
            (await unknownEmail.ConfirmAsync("nadie@email.com")).Error,
            (await wrongCode.ConfirmAsync(code: "000000")).Error,
            (await expired.ConfirmAsync()).Error,
            (await used.ConfirmAsync()).Error,
            (await revoked.ConfirmAsync()).Error,
            (await exhausted.ConfirmAsync()).Error,
            (await verified.ConfirmAsync()).Error,
            (await suspended.ConfirmAsync()).Error
        ];

        Assert.All(errors, error => Assert.Equal(errors[0], error));
    }

    // --- Intentos fallidos ---

    [Fact]
    public async Task Confirm_WithAWrongCode_RegistersAndPersistsTheFailedAttempt()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        await ConfirmAsync(code: "000000");

        Assert.Equal(1, verification.FailedAttempts);
        Assert.True(_writer.FailedAttemptCommitted);
        Assert.Same(verification, _writer.FailedVerification);
    }

    [Fact]
    public async Task Confirm_AtTheMaximumAttempt_RevokesTheCode()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        for (int attempt = 0; attempt < _defaults.MaximumFailedAttempts; attempt++)
        {
            AssertInvalidCode(await ConfirmAsync(code: "000000"));
        }

        Assert.Equal(_defaults.MaximumFailedAttempts, verification.FailedAttempts);
        Assert.True(verification.IsRevoked);
        Assert.Equal(UtcNow.UtcDateTime, verification.RevokedAtUtc);
    }

    /// <summary>
    /// Agotados los intentos, ni siquiera el código correcto sirve: hay que pedir uno nuevo.
    /// </summary>
    [Fact]
    public async Task Confirm_AfterExhaustingTheAttempts_RejectsEvenTheCorrectCode()
    {
        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        for (int attempt = 0; attempt < _defaults.MaximumFailedAttempts; attempt++)
        {
            await ConfirmAsync(code: "000000");
        }

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(verification.IsUsed);
        Assert.Equal(UserStatus.Unverified, user.Status);
        Assert.False(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_RespectsTheConfiguredMaximumAttempts()
    {
        _defaults.MaximumFailedAttempts = 2;

        User user = SeedUser();
        UserEmailVerification verification = SeedVerification(user.Id);

        await ConfirmAsync(code: "000000");
        Assert.False(verification.IsRevoked);

        await ConfirmAsync(code: "000000");
        Assert.True(verification.IsRevoked);
    }

    // --- Concurrencia y fallos de persistencia ---

    /// <summary>
    /// Si otra solicitud canjeó el código primero, la confirmación se rechaza con el
    /// mismo error genérico y el usuario no se activa dos veces.
    /// </summary>
    [Fact]
    public async Task Confirm_WhenAnotherRequestUsedTheCodeFirst_ReturnsInvalidCode()
    {
        User user = SeedUser();
        SeedVerification(user.Id);
        _writer.SimulateConcurrentConfirmation = true;

        AssertInvalidCode(await ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_WhenThePersistenceFails_ThePropagatedErrorIsNotSwallowed()
    {
        User user = SeedUser();
        SeedVerification(user.Id);
        _writer.FailureToThrow = new InvalidOperationException("fallo de base de datos");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfirmAsync());

        Assert.False(_writer.ConfirmationCommitted);
    }

    [Fact]
    public async Task Confirm_WhenTheFailedAttemptCannotBePersisted_TheErrorIsNotSwallowed()
    {
        User user = SeedUser();
        SeedVerification(user.Id);
        _writer.FailureToThrow = new InvalidOperationException("fallo de base de datos");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfirmAsync(code: "000000"));

        Assert.False(_writer.FailedAttemptCommitted);
        Assert.Equal(UserStatus.Unverified, user.Status);
    }
}
