using System.Security.Cryptography;
using System.Text;
using Lyria.Domain.Users.EmailVerifications;
using Lyria.Infrastructure.Notifications;
using Lyria.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lyria.Infrastructure.IntegrationTests.Notifications;

/// <summary>
/// Comprueba la generación del código, su hash y la composición del mensaje.
/// </summary>
public sealed class EmailVerificationSecurityTests
{
    private const string Secret = "un-secreto-de-pruebas-suficientemente-largo-1234";

    private static EmailVerificationCodeGenerator CreateGenerator(int codeLength = 6) =>
        new(Options.Create(new EmailVerificationOptions
        {
            CodeSecret = Secret,
            CodeLength = codeLength
        }));

    private static EmailVerificationCodeHasher CreateHasher(string secret = Secret) =>
        new(Options.Create(new EmailVerificationOptions { CodeSecret = secret }));

    // --- Generación ---

    [Fact]
    public void Generate_AlwaysProducesSixDigits()
    {
        EmailVerificationCodeGenerator generator = CreateGenerator();

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            string code = generator.Generate();

            Assert.Equal(6, code.Length);
            Assert.All(code, character => Assert.True(char.IsAsciiDigit(character)));
        }
    }

    [Fact]
    public void Generate_StaysWithinTheAuthorizedRange()
    {
        EmailVerificationCodeGenerator generator = CreateGenerator();

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            int value = int.Parse(
                generator.Generate(), System.Globalization.CultureInfo.InvariantCulture);

            Assert.InRange(value, 0, 999_999);
        }
    }

    /// <summary>
    /// Un código puede empezar por cero, y el cero inicial es significativo.
    /// </summary>
    [Fact]
    public void Generate_PreservesLeadingZeros()
    {
        EmailVerificationCodeGenerator generator = CreateGenerator();

        string[] codes = [.. Enumerable.Range(0, 20_000).Select(_ => generator.Generate())];

        Assert.Contains(codes, code => code[0] == '0');
        Assert.All(codes, code => Assert.Equal(6, code.Length));
    }

    /// <summary>
    /// La fuente es criptográfica: dos secuencias consecutivas no pueden coincidir.
    /// </summary>
    [Fact]
    public void Generate_IsNotASequence()
    {
        EmailVerificationCodeGenerator generator = CreateGenerator();

        string[] codes = [.. Enumerable.Range(0, 200).Select(_ => generator.Generate())];

        Assert.True(codes.Distinct(StringComparer.Ordinal).Count() > 100);
    }

    [Fact]
    public void Generate_WithAnUnsupportedCodeLength_Throws()
    {
        EmailVerificationCodeGenerator generator = CreateGenerator(codeLength: 8);

        Assert.Throws<InvalidOperationException>(generator.Generate);
    }

    // --- Hash ---

    [Fact]
    public void ComputeHash_ProducesA64CharacterLowercaseHexadecimal()
    {
        string hash = CreateHasher().ComputeHash("482731");

        Assert.Equal(UserEmailVerification.CodeHashLength, hash.Length);
        Assert.All(hash, character =>
            Assert.True(character is (>= '0' and <= '9') or (>= 'a' and <= 'f')));
    }

    /// <summary>
    /// El hash es exactamente el HMAC-SHA256 del código con el secreto configurado.
    /// </summary>
    [Fact]
    public void ComputeHash_IsTheHmacOfTheCodeWithTheConfiguredSecret()
    {
        const string code = "482731";

        string expected = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(code)));

        Assert.Equal(expected, CreateHasher().ComputeHash(code));
    }

    /// <summary>
    /// Sin el secreto no se puede reproducir el hash: es lo que protege los códigos si la
    /// base de datos se filtra.
    /// </summary>
    [Fact]
    public void ComputeHash_WithADifferentSecret_ProducesADifferentHash()
    {
        const string code = "482731";

        Assert.NotEqual(
            CreateHasher().ComputeHash(code),
            CreateHasher("otro-secreto-de-pruebas-igual-de-largo-98765").ComputeHash(code));
    }

    [Fact]
    public void ComputeHash_NeverContainsThePlainCode()
    {
        const string code = "482731";

        Assert.DoesNotContain(code, CreateHasher().ComputeHash(code), StringComparison.Ordinal);
    }

    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        EmailVerificationCodeHasher hasher = CreateHasher();

        Assert.Equal(hasher.ComputeHash("000000"), hasher.ComputeHash("000000"));
        Assert.NotEqual(hasher.ComputeHash("000000"), hasher.ComputeHash("000001"));
    }

    /// <summary>
    /// El hash producido cumple el formato que exige el dominio.
    /// </summary>
    [Fact]
    public void ComputeHash_ProducesAHashTheDomainAccepts()
    {
        UserEmailVerification verification = UserEmailVerification.Create(
            UserEmailVerificationId.New(),
            new Domain.Users.UserId(Guid.NewGuid()),
            CreateHasher().ComputeHash("482731"),
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15));

        Assert.Equal(UserEmailVerification.CodeHashLength, verification.CodeHash.Length);
    }

    // --- Comparación ---

    [Fact]
    public void Matches_WithTheCorrectCode_ReturnsTrue()
    {
        EmailVerificationCodeHasher hasher = CreateHasher();

        Assert.True(hasher.Matches(hasher.ComputeHash("482731"), "482731"));
    }

    [Theory]
    [InlineData("482730")]
    [InlineData("000000")]
    [InlineData("")]
    [InlineData("4827310")]
    public void Matches_WithAWrongCode_ReturnsFalse(string code)
    {
        EmailVerificationCodeHasher hasher = CreateHasher();

        Assert.False(hasher.Matches(hasher.ComputeHash("482731"), code));
    }

    [Fact]
    public void Matches_WithAMalformedStoredHash_ReturnsFalse()
    {
        EmailVerificationCodeHasher hasher = CreateHasher();

        Assert.False(hasher.Matches("no-es-un-hash", "482731"));
        Assert.False(hasher.Matches(string.Empty, "482731"));
    }

    [Fact]
    public void Matches_IsCaseInsensitiveForTheStoredHash()
    {
        EmailVerificationCodeHasher hasher = CreateHasher();

        string hash = hasher.ComputeHash("482731");

        Assert.True(hasher.Matches(hash.ToUpperInvariant(), "482731"));
    }

    // --- Plantilla del mensaje ---

    [Fact]
    public void Template_UsesTheAuthorizedSubject()
    {
        Assert.Equal(
            "Verifica tu correo electrónico en Lyria",
            EmailVerificationMessageTemplate.Subject);
    }

    [Fact]
    public void Template_IncludesTheNameCodeAndExpiration()
    {
        DateTime expiresAtUtc = new(2026, 8, 5, 10, 15, 0, DateTimeKind.Utc);

        string text = EmailVerificationMessageTemplate.BuildTextBody(
            "Andres", "482731", expiresAtUtc);

        Assert.Contains("Hola Andres,", text, StringComparison.Ordinal);
        Assert.Contains("482731", text, StringComparison.Ordinal);
        Assert.Contains("05/08/2026 10:15 UTC", text, StringComparison.Ordinal);
        Assert.Contains("solo puede utilizarse una vez", text, StringComparison.Ordinal);
        Assert.Contains("Equipo Lyria", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un nombre con marcado no puede alterar la estructura del mensaje HTML.
    /// </summary>
    [Fact]
    public void Template_EscapesTheRecipientNameInHtml()
    {
        string html = EmailVerificationMessageTemplate.BuildHtmlBody(
            "<script>alert('x')</script>", "482731", DateTime.UtcNow.AddMinutes(15));

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// El nombre se escapa por completo —incluidos los acentos, que se codifican como
    /// referencias numéricas— sin perder ningún carácter del original.
    /// </summary>
    [Fact]
    public void Template_EscapesAccentedAndSpecialCharactersWithoutLosingTheName()
    {
        string html = EmailVerificationMessageTemplate.BuildHtmlBody(
            "María & Asociados", "482731", DateTime.UtcNow.AddMinutes(15));

        Assert.Contains("Mar&#237;a &amp; Asociados", html, StringComparison.Ordinal);
        Assert.DoesNotContain("María & Asociados", html, StringComparison.Ordinal);

        // La versión en texto plano no necesita escape y conserva el nombre tal cual.
        string text = EmailVerificationMessageTemplate.BuildTextBody(
            "María & Asociados", "482731", DateTime.UtcNow.AddMinutes(15));

        Assert.Contains("Hola María & Asociados,", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// El mensaje no puede llevar credenciales, tokens ni enlaces.
    /// </summary>
    [Theory]
    [InlineData("password")]
    [InlineData("contraseña")]
    [InlineData("accessToken")]
    [InlineData("refreshToken")]
    [InlineData("http://")]
    public void Template_DoesNotIncludeAnyForbiddenContent(string forbidden)
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        string text = EmailVerificationMessageTemplate.BuildTextBody(
            "Andres", "482731", expiresAtUtc);
        string html = EmailVerificationMessageTemplate.BuildHtmlBody(
            "Andres", "482731", expiresAtUtc);

        Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(forbidden, html, StringComparison.OrdinalIgnoreCase);
    }
}
