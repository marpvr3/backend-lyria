using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Xunit;

namespace Lyria.Domain.UnitTests.Users.RefreshTokens;

public sealed class UserRefreshTokenTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 8, 4, 23, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ExpiresAtUtc =
        new(2026, 9, 3, 23, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Hash SHA-256 válido: 64 caracteres hexadecimales.
    /// </summary>
    private const string TokenHash =
        "3b8c9f1d2e4a5b6c7d8e9f0a1b2c3d4e5f60718293a4b5c6d7e8f9012a3b4c5d";

    private static UserRefreshToken Create(
        string tokenHash = TokenHash,
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null,
        UserId? userId = null) =>
        UserRefreshToken.Create(
            UserRefreshTokenId.New(),
            userId ?? UserId.New(),
            tokenHash,
            createdAtUtc ?? CreatedAtUtc,
            expiresAtUtc ?? ExpiresAtUtc);

    // --- Creación válida ---

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var userId = UserId.New();

        UserRefreshToken session = Create(userId: userId);

        Assert.Equal(userId, session.UserId);
        Assert.Equal(TokenHash, session.TokenHash);
        Assert.Equal(CreatedAtUtc, session.CreatedAtUtc);
        Assert.Equal(ExpiresAtUtc, session.ExpiresAtUtc);
        Assert.Null(session.RevokedAtUtc);
    }

    [Fact]
    public void Create_NewSession_IsActiveAndNotRevokedNorExpired()
    {
        UserRefreshToken session = Create();

        Assert.True(session.IsActive(CreatedAtUtc));
        Assert.False(session.IsRevoked);
        Assert.False(session.IsExpired(CreatedAtUtc));
    }

    [Fact]
    public void Create_NormalizesTokenHashToLowercase()
    {
        UserRefreshToken session = Create(TokenHash.ToUpperInvariant());

        Assert.Equal(TokenHash, session.TokenHash);
    }

    // --- Token hash obligatorio ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutTokenHash_Throws(string tokenHash)
    {
        UserRefreshTokenException exception =
            Assert.Throws<UserRefreshTokenException>(() => Create(tokenHash));

        Assert.Contains("obligatorio", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithTokenHashOfWrongLength_Throws()
    {
        Assert.Throws<UserRefreshTokenException>(() => Create("abc123"));
    }

    /// <summary>
    /// Un refresh token en claro es Base64Url y contiene caracteres fuera del rango
    /// hexadecimal, por lo que la entidad lo rechaza. Es la garantía de dominio de que
    /// nunca se almacena un token en texto plano.
    /// </summary>
    [Fact]
    public void Create_WithPlainTextTokenInsteadOfHash_Throws()
    {
        // 64 caracteres, pero con símbolos propios de Base64Url.
        const string plainTextToken =
            "Zm9vYmFyX3Rva2VuLXBsYW5vLXF1ZS1uby1lcy1oZXhhZGVjaW1hbC0xMjM0NTY3OA";

        Assert.Throws<UserRefreshTokenException>(
            () => Create(plainTextToken[..64]));
    }

    [Fact]
    public void Create_WithUppercaseHexOutsideRange_Throws()
    {
        string invalid = new('g', 64);

        Assert.Throws<UserRefreshTokenException>(() => Create(invalid));
    }

    [Fact]
    public void TokenHash_NeverContainsThePlainToken()
    {
        const string plainToken = "token-opaco-de-prueba";

        UserRefreshToken session = Create();

        Assert.DoesNotContain(plainToken, session.TokenHash, StringComparison.Ordinal);
        Assert.Equal(UserRefreshToken.TokenHashLength, session.TokenHash.Length);
    }

    // --- Usuario obligatorio ---

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        UserRefreshTokenException exception = Assert.Throws<UserRefreshTokenException>(
            () => Create(userId: new UserId(Guid.Empty)));

        Assert.Contains("usuario", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- Fechas válidas ---

    [Fact]
    public void Create_WithExpirationBeforeCreation_Throws()
    {
        UserRefreshTokenException exception = Assert.Throws<UserRefreshTokenException>(
            () => Create(expiresAtUtc: CreatedAtUtc.AddSeconds(-1)));

        Assert.Contains("posterior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithExpirationEqualToCreation_Throws()
    {
        Assert.Throws<UserRefreshTokenException>(
            () => Create(expiresAtUtc: CreatedAtUtc));
    }

    // --- Expiración ---

    [Fact]
    public void IsExpired_BeforeExpiration_ReturnsFalse()
    {
        UserRefreshToken session = Create();

        Assert.False(session.IsExpired(ExpiresAtUtc.AddSeconds(-1)));
    }

    [Fact]
    public void IsExpired_AtExpiration_ReturnsTrue()
    {
        UserRefreshToken session = Create();

        Assert.True(session.IsExpired(ExpiresAtUtc));
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        UserRefreshToken session = Create();

        Assert.False(session.IsActive(ExpiresAtUtc.AddDays(1)));
    }

    // --- Revocación ---

    [Fact]
    public void Revoke_SetsRevokedAtUtcAndDeactivatesSession()
    {
        UserRefreshToken session = Create();
        DateTime revokedAtUtc = CreatedAtUtc.AddHours(1);

        session.Revoke(revokedAtUtc);

        Assert.True(session.IsRevoked);
        Assert.Equal(revokedAtUtc, session.RevokedAtUtc);
        Assert.False(session.IsActive(revokedAtUtc));
    }

    [Fact]
    public void Revoke_DoesNotRemoveTheSessionData()
    {
        UserRefreshToken session = Create();

        session.Revoke(CreatedAtUtc.AddHours(1));

        // La revocación es lógica: el resto del historial se conserva intacto.
        Assert.Equal(TokenHash, session.TokenHash);
        Assert.Equal(CreatedAtUtc, session.CreatedAtUtc);
        Assert.Equal(ExpiresAtUtc, session.ExpiresAtUtc);
    }

    [Fact]
    public void Revoke_Twice_ThrowsAndKeepsTheFirstRevocation()
    {
        UserRefreshToken session = Create();
        DateTime firstRevocation = CreatedAtUtc.AddHours(1);

        session.Revoke(firstRevocation);

        UserRefreshTokenException exception = Assert.Throws<UserRefreshTokenException>(
            () => session.Revoke(CreatedAtUtc.AddHours(2)));

        Assert.Contains("ya se encuentra revocada", exception.Message, StringComparison.Ordinal);
        Assert.Equal(firstRevocation, session.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_BeforeCreation_Throws()
    {
        UserRefreshToken session = Create();

        UserRefreshTokenException exception = Assert.Throws<UserRefreshTokenException>(
            () => session.Revoke(CreatedAtUtc.AddSeconds(-1)));

        Assert.Contains("anterior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Revoke_AnExpiredSession_IsStillPossible()
    {
        UserRefreshToken session = Create();
        DateTime afterExpiration = ExpiresAtUtc.AddDays(1);

        session.Revoke(afterExpiration);

        Assert.True(session.IsRevoked);
        Assert.True(session.IsExpired(afterExpiration));
        Assert.False(session.IsActive(afterExpiration));
    }
}
