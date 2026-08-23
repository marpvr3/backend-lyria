using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.EmailVerifications;

public sealed class UserEmailVerificationTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc = CreatedAtUtc.AddMinutes(15);

    private static readonly string ValidHash = new('a', UserEmailVerification.CodeHashLength);

    private static UserEmailVerification Create(
        string? codeHash = null,
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null,
        UserId? userId = null) =>
        UserEmailVerification.Create(
            UserEmailVerificationId.New(),
            userId ?? UserId.New(),
            codeHash ?? ValidHash,
            createdAtUtc ?? CreatedAtUtc,
            expiresAtUtc ?? ExpiresAtUtc);

    // --- Creación ---

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var userId = UserId.New();

        UserEmailVerification verification = Create(userId: userId);

        Assert.Equal(userId, verification.UserId);
        Assert.Equal(ValidHash, verification.CodeHash);
        Assert.Equal(CreatedAtUtc, verification.CreatedAtUtc);
        Assert.Equal(ExpiresAtUtc, verification.ExpiresAtUtc);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var exception = Assert.Throws<UserEmailVerificationException>(
            () => Create(userId: new UserId(Guid.Empty)));

        Assert.Contains("usuario", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCodeHash_Throws(string codeHash)
    {
        Assert.Throws<UserEmailVerificationException>(() => Create(codeHash));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("482731")]
    public void Create_WithCodeHashOfWrongLength_Throws(string codeHash)
    {
        var exception = Assert.Throws<UserEmailVerificationException>(() => Create(codeHash));

        Assert.Contains("64", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Exigir hexadecimal impide almacenar por error el código en claro.
    /// </summary>
    [Fact]
    public void Create_WithNonHexadecimalCodeHash_Throws()
    {
        string invalid = new('z', UserEmailVerification.CodeHashLength);

        var exception = Assert.Throws<UserEmailVerificationException>(() => Create(invalid));

        Assert.Contains("hexadecimal", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_NormalizesTheCodeHashToLowercase()
    {
        string upper = new('A', UserEmailVerification.CodeHashLength);

        UserEmailVerification verification = Create(upper);

        Assert.Equal(ValidHash, verification.CodeHash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithExpirationNotAfterCreation_Throws(int minutes)
    {
        var exception = Assert.Throws<UserEmailVerificationException>(
            () => Create(expiresAtUtc: CreatedAtUtc.AddMinutes(minutes)));

        Assert.Contains("expiración", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- Estado inicial ---

    [Fact]
    public void Create_LeavesTheVerificationActive()
    {
        UserEmailVerification verification = Create();

        Assert.False(verification.IsUsed);
        Assert.False(verification.IsRevoked);
        Assert.False(verification.IsExpired(CreatedAtUtc));
        Assert.False(verification.HasExceededMaximumAttempts());
        Assert.True(verification.IsActive(CreatedAtUtc));
        Assert.Equal(0, verification.FailedAttempts);
        Assert.Null(verification.UsedAtUtc);
        Assert.Null(verification.RevokedAtUtc);
    }

    // --- Propiedades derivadas ---

    [Fact]
    public void IsExpired_IsTrueFromTheExpirationInstant()
    {
        UserEmailVerification verification = Create();

        Assert.False(verification.IsExpired(ExpiresAtUtc.AddTicks(-1)));
        Assert.True(verification.IsExpired(ExpiresAtUtc));
        Assert.True(verification.IsExpired(ExpiresAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void IsActive_IsFalseOnceExpired()
    {
        UserEmailVerification verification = Create();

        Assert.False(verification.IsActive(ExpiresAtUtc));
    }

    [Fact]
    public void IsActive_IsFalseOnceUsed()
    {
        UserEmailVerification verification = Create();
        verification.MarkAsUsed(CreatedAtUtc.AddMinutes(1));

        Assert.False(verification.IsActive(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void IsActive_IsFalseOnceRevoked()
    {
        UserEmailVerification verification = Create();
        verification.Revoke(CreatedAtUtc.AddMinutes(1));

        Assert.False(verification.IsActive(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void IsActive_IsFalseOnceTheAttemptsAreExhausted()
    {
        UserEmailVerification verification = Create();

        for (int attempt = 0; attempt < UserEmailVerification.DefaultMaximumFailedAttempts;
             attempt++)
        {
            verification.RegisterFailedAttempt();
        }

        Assert.False(verification.IsActive(CreatedAtUtc.AddMinutes(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HasExceededMaximumAttempts_WithANonPositiveMaximum_Throws(int maximum)
    {
        UserEmailVerification verification = Create();

        Assert.Throws<UserEmailVerificationException>(
            () => verification.HasExceededMaximumAttempts(maximum));
    }

    // --- Intentos fallidos ---

    [Fact]
    public void RegisterFailedAttempt_IncrementsTheCounter()
    {
        UserEmailVerification verification = Create();

        verification.RegisterFailedAttempt();
        verification.RegisterFailedAttempt();

        Assert.Equal(2, verification.FailedAttempts);
    }

    [Fact]
    public void RegisterFailedAttempt_MarksTheMaximumOnlyWhenReached()
    {
        UserEmailVerification verification = Create();

        for (int attempt = 1; attempt < 5; attempt++)
        {
            verification.RegisterFailedAttempt();
            Assert.False(verification.HasExceededMaximumAttempts(5));
        }

        verification.RegisterFailedAttempt();

        Assert.True(verification.HasExceededMaximumAttempts(5));
    }

    [Fact]
    public void RegisterFailedAttempt_RespectsAConfiguredMaximum()
    {
        UserEmailVerification verification = Create();

        verification.RegisterFailedAttempt();
        verification.RegisterFailedAttempt();

        Assert.True(verification.HasExceededMaximumAttempts(2));
        Assert.False(verification.HasExceededMaximumAttempts(3));
    }

    [Fact]
    public void RegisterFailedAttempt_OnAUsedVerification_Throws()
    {
        UserEmailVerification verification = Create();
        verification.MarkAsUsed(CreatedAtUtc.AddMinutes(1));

        Assert.Throws<UserEmailVerificationException>(verification.RegisterFailedAttempt);
    }

    [Fact]
    public void RegisterFailedAttempt_OnARevokedVerification_Throws()
    {
        UserEmailVerification verification = Create();
        verification.Revoke(CreatedAtUtc.AddMinutes(1));

        Assert.Throws<UserEmailVerificationException>(verification.RegisterFailedAttempt);
    }

    // --- Uso ---

    [Fact]
    public void MarkAsUsed_SetsTheUsageDate()
    {
        UserEmailVerification verification = Create();
        DateTime usedAtUtc = CreatedAtUtc.AddMinutes(3);

        verification.MarkAsUsed(usedAtUtc);

        Assert.True(verification.IsUsed);
        Assert.Equal(usedAtUtc, verification.UsedAtUtc);
    }

    [Fact]
    public void MarkAsUsed_Twice_Throws()
    {
        UserEmailVerification verification = Create();
        verification.MarkAsUsed(CreatedAtUtc.AddMinutes(1));

        var exception = Assert.Throws<UserEmailVerificationException>(
            () => verification.MarkAsUsed(CreatedAtUtc.AddMinutes(2)));

        Assert.Contains("ya fue utilizado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkAsUsed_WhenExpired_Throws()
    {
        UserEmailVerification verification = Create();

        var exception = Assert.Throws<UserEmailVerificationException>(
            () => verification.MarkAsUsed(ExpiresAtUtc));

        Assert.Contains("vencido", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(verification.IsUsed);
    }

    [Fact]
    public void MarkAsUsed_WhenRevoked_Throws()
    {
        UserEmailVerification verification = Create();
        verification.Revoke(CreatedAtUtc.AddMinutes(1));

        var exception = Assert.Throws<UserEmailVerificationException>(
            () => verification.MarkAsUsed(CreatedAtUtc.AddMinutes(2)));

        Assert.Contains("revocado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkAsUsed_BeforeCreation_Throws()
    {
        UserEmailVerification verification = Create();

        Assert.Throws<UserEmailVerificationException>(
            () => verification.MarkAsUsed(CreatedAtUtc.AddMinutes(-1)));
    }

    [Fact]
    public void MarkAsUsed_WithExhaustedAttempts_StillRecordsTheUsage()
    {
        // La regla de intentos la aplica el caso de uso a partir de la configuración; el
        // dominio no la impone al canjear porque el máximo no es una constante fija.
        UserEmailVerification verification = Create();
        verification.RegisterFailedAttempt();

        verification.MarkAsUsed(CreatedAtUtc.AddMinutes(1));

        Assert.True(verification.IsUsed);
    }

    // --- Revocación ---

    [Fact]
    public void Revoke_SetsTheRevocationDate()
    {
        UserEmailVerification verification = Create();
        DateTime revokedAtUtc = CreatedAtUtc.AddMinutes(2);

        verification.Revoke(revokedAtUtc);

        Assert.True(verification.IsRevoked);
        Assert.Equal(revokedAtUtc, verification.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_Twice_Throws()
    {
        UserEmailVerification verification = Create();
        verification.Revoke(CreatedAtUtc.AddMinutes(1));

        Assert.Throws<UserEmailVerificationException>(
            () => verification.Revoke(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void Revoke_AUsedVerification_Throws()
    {
        UserEmailVerification verification = Create();
        verification.MarkAsUsed(CreatedAtUtc.AddMinutes(1));

        var exception = Assert.Throws<UserEmailVerificationException>(
            () => verification.Revoke(CreatedAtUtc.AddMinutes(2)));

        Assert.Contains("utilizado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Revoke_BeforeCreation_Throws()
    {
        UserEmailVerification verification = Create();

        Assert.Throws<UserEmailVerificationException>(
            () => verification.Revoke(CreatedAtUtc.AddMinutes(-1)));
    }

    /// <summary>
    /// Un código vencido sí puede revocarse: la revocación es la forma de cerrarlo de
    /// manera explícita cuando se emite uno nuevo.
    /// </summary>
    [Fact]
    public void Revoke_AnExpiredVerification_Succeeds()
    {
        UserEmailVerification verification = Create();

        verification.Revoke(ExpiresAtUtc.AddMinutes(1));

        Assert.True(verification.IsRevoked);
    }

    // --- La entidad no guarda datos sensibles ---

    [Fact]
    public void TheEntity_DoesNotExposeAnySensitiveMember()
    {
        string[] properties = [.. typeof(UserEmailVerification)
            .GetProperties()
            .Select(p => p.Name)];

        foreach (string forbidden in new[]
        {
            "Code", "Email", "Password", "PasswordHash", "Token",
            "AccessToken", "RefreshToken", "Smtp"
        })
        {
            Assert.DoesNotContain(forbidden, properties, StringComparer.Ordinal);
        }
    }
}
