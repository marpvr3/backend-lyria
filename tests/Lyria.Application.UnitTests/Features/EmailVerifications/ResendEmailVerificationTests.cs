using Lyria.Application.Common.Results;
using Lyria.Application.Features.EmailVerifications;
using Lyria.Application.Features.EmailVerifications.Resend;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lyria.Application.UnitTests.Features.EmailVerifications;

public sealed class ResendEmailVerificationTests
{
    private const string Email = "andres@email.com";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserEmailVerificationRepository _verifications = new();
    private readonly FakeEmailVerificationWriter _writer = new();
    private readonly FakeEmailVerificationCodeGenerator _codeGenerator = new();
    private readonly FakeEmailVerificationCodeHasher _codeHasher = new();
    private readonly FakeEmailVerificationDefaults _defaults = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FixedTimeProvider _timeProvider = new(UtcNow);

    private ResendEmailVerificationCommandHandler CreateHandler() =>
        new(_users, _verifications, _writer, _codeGenerator, _codeHasher,
            _defaults, _emailSender, _timeProvider,
            NullLogger<ResendEmailVerificationCommandHandler>.Instance);

    private User SeedUser(UserStatus status = UserStatus.Unverified, string email = Email)
    {
        var user = User.Create(
            UserId.New(), "Andres", "Perez", email, "hash", null, null, null);

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
        DateTime? createdAtUtc = null)
    {
        DateTime created = createdAtUtc ?? UtcNow.UtcDateTime.AddHours(-1);

        var verification = UserEmailVerification.Create(
            UserEmailVerificationId.New(),
            userId,
            _codeHasher.ComputeHash("111111"),
            created,
            created.AddMinutes(15));

        _verifications.Seed(verification);

        return verification;
    }

    private async Task<Result<EmailVerificationResendResponse>> ResendAsync(
        string email = Email) =>
        await CreateHandler().Handle(
            new ResendEmailVerificationCommand(email), TestContext.Current.CancellationToken);

    // --- Camino feliz ---

    [Fact]
    public async Task Resend_ForAnUnverifiedUser_IssuesANewCodeAndSendsIt()
    {
        User user = SeedUser();

        Result<EmailVerificationResendResponse> result = await ResendAsync();

        Assert.True(result.IsSuccess);
        Assert.True(_writer.ResendCommitted);

        UserEmailVerification created =
            Assert.IsType<UserEmailVerification>(_writer.CreatedVerification);

        Assert.Equal(user.Id, created.UserId);
        Assert.Equal(UtcNow.UtcDateTime, created.CreatedAtUtc);
        Assert.Equal(
            UtcNow.UtcDateTime.AddMinutes(_defaults.ExpirationMinutes), created.ExpiresAtUtc);

        FakeEmailSender.SentEmail sent = Assert.Single(_emailSender.SentEmails);
        Assert.Equal(Email, sent.RecipientEmail);
        Assert.Equal(Assert.Single(_codeGenerator.GeneratedCodes), sent.VerificationCode);
    }

    [Fact]
    public async Task Resend_PersistsOnlyTheCodeHash()
    {
        SeedUser();

        await ResendAsync();

        UserEmailVerification created =
            Assert.IsType<UserEmailVerification>(_writer.CreatedVerification);

        string code = Assert.Single(_codeGenerator.GeneratedCodes);

        Assert.NotEqual(code, created.CodeHash);
        Assert.Equal(_codeHasher.ComputeHash(code), created.CodeHash);
    }

    [Fact]
    public async Task Resend_RevokesEveryPendingVerification()
    {
        User user = SeedUser();
        UserEmailVerification first = SeedVerification(
            user.Id, UtcNow.UtcDateTime.AddHours(-2));
        UserEmailVerification second = SeedVerification(
            user.Id, UtcNow.UtcDateTime.AddHours(-1));

        await ResendAsync();

        Assert.True(first.IsRevoked);
        Assert.True(second.IsRevoked);
        Assert.Equal(UtcNow.UtcDateTime, first.RevokedAtUtc);
        Assert.Equal(2, _writer.RevokedVerifications.Count);
    }

    /// <summary>
    /// El envío nunca puede ocurrir con la transacción abierta.
    /// </summary>
    [Fact]
    public async Task Resend_SendsTheEmailAfterTheDatabaseWriteIsCommitted()
    {
        SeedUser();

        bool committedWhenSending = false;
        _emailSender.OnSend = () => committedWhenSending = _writer.ResendCommitted;

        await ResendAsync();

        Assert.True(committedWhenSending);
    }

    [Fact]
    public async Task Resend_NormalizesTheEmailBeforeLookup()
    {
        SeedUser();

        await ResendAsync("  ANDRES@EMAIL.COM  ");

        Assert.True(_writer.ResendCommitted);
    }

    // --- Respuestas indistinguibles ---

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deleted)]
    public async Task Resend_ForANonUnverifiedUser_DoesNothing(UserStatus status)
    {
        SeedUser(status);

        Result<EmailVerificationResendResponse> result = await ResendAsync();

        Assert.True(result.IsSuccess);
        Assert.False(_writer.ResendCommitted);
        Assert.Empty(_emailSender.SentEmails);
    }

    [Fact]
    public async Task Resend_ForAnUnknownEmail_DoesNothing()
    {
        Result<EmailVerificationResendResponse> result = await ResendAsync("nadie@email.com");

        Assert.True(result.IsSuccess);
        Assert.False(_writer.ResendCommitted);
        Assert.Empty(_emailSender.SentEmails);
    }

    /// <summary>
    /// Los cinco escenarios deben producir exactamente la misma respuesta: ninguna revela
    /// si la cuenta existe ni en qué estado está.
    /// </summary>
    [Fact]
    public async Task Resend_AllOutcomes_ProduceAnIdenticalResponse()
    {
        var unknown = new ResendEmailVerificationTests();

        var verified = new ResendEmailVerificationTests();
        verified.SeedUser(UserStatus.Active);

        var suspended = new ResendEmailVerificationTests();
        suspended.SeedUser(UserStatus.Suspended);

        var deleted = new ResendEmailVerificationTests();
        deleted.SeedUser(UserStatus.Deleted);

        var pending = new ResendEmailVerificationTests();
        pending.SeedUser();

        EmailVerificationResendResponse[] responses =
        [
            (await unknown.ResendAsync()).Value,
            (await verified.ResendAsync()).Value,
            (await suspended.ResendAsync()).Value,
            (await deleted.ResendAsync()).Value,
            (await pending.ResendAsync()).Value
        ];

        Assert.All(responses, response => Assert.Equal(responses[0], response));
        Assert.Equal(EmailVerificationResendResponse.GenericMessage, responses[0].Message);
    }

    // --- Intervalo mínimo entre envíos ---

    [Fact]
    public async Task Resend_WithinTheCooldown_DoesNotIssueANewCode()
    {
        User user = SeedUser();
        SeedVerification(user.Id, UtcNow.UtcDateTime.AddSeconds(-30));

        Result<EmailVerificationResendResponse> result = await ResendAsync();

        Assert.True(result.IsSuccess);
        Assert.False(_writer.ResendCommitted);
        Assert.Empty(_emailSender.SentEmails);
    }

    [Fact]
    public async Task Resend_OnceTheCooldownElapsed_IssuesANewCode()
    {
        User user = SeedUser();
        SeedVerification(
            user.Id, UtcNow.UtcDateTime.AddSeconds(-_defaults.ResendCooldownSeconds));

        await ResendAsync();

        Assert.True(_writer.ResendCommitted);
        Assert.Single(_emailSender.SentEmails);
    }

    /// <summary>
    /// La respuesta del intervalo mínimo es idéntica a la de un envío efectivo.
    /// </summary>
    [Fact]
    public async Task Resend_WithinTheCooldown_ProducesTheSameResponseAsASuccessfulSend()
    {
        User user = SeedUser();
        SeedVerification(user.Id, UtcNow.UtcDateTime.AddSeconds(-30));

        Result<EmailVerificationResendResponse> throttled = await ResendAsync();

        var sending = new ResendEmailVerificationTests();
        sending.SeedUser();
        Result<EmailVerificationResendResponse> sent = await sending.ResendAsync();

        Assert.Equal(sent.Value, throttled.Value);
    }

    // --- Fallo del proveedor de correo ---

    [Fact]
    public async Task Resend_WhenTheEmailFails_TheCodeRemainsIssued()
    {
        SeedUser();
        _emailSender.FailureToThrow = new InvalidOperationException("fallo del proveedor SMTP");

        Result<EmailVerificationResendResponse> result = await ResendAsync();

        // La respuesta no cambia y el código emitido sigue siendo canjeable.
        Assert.True(result.IsSuccess);
        Assert.True(_writer.ResendCommitted);
        Assert.NotNull(_writer.CreatedVerification);
    }

    [Fact]
    public async Task Resend_WhenThePersistenceFails_NothingIsCommittedAndNoEmailIsSent()
    {
        SeedUser();
        _writer.FailureToThrow = new InvalidOperationException("fallo de base de datos");

        await Assert.ThrowsAsync<InvalidOperationException>(() => ResendAsync());

        Assert.False(_writer.ResendCommitted);
        Assert.Empty(_emailSender.SentEmails);
    }

    // --- Respuesta ---

    [Fact]
    public async Task Resend_ResponseNeverContainsTheCode()
    {
        SeedUser();

        Result<EmailVerificationResendResponse> result = await ResendAsync();

        string serialized = System.Text.Json.JsonSerializer.Serialize(result.Value);
        string code = Assert.Single(_codeGenerator.GeneratedCodes);

        Assert.DoesNotContain(code, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(
            _codeHasher.ComputeHash(code), serialized, StringComparison.OrdinalIgnoreCase);
    }
}
