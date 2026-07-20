using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments;

public sealed class EstablishmentCreateTests
{
    private static readonly EstablishmentId DefaultId = EstablishmentId.New();
    private static readonly EstablishmentCategoryId DefaultCategoryId = EstablishmentCategoryId.New();

    [Fact]
    public void Create_WithValidData_CreatesEstablishment()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Let It V", "let-it-v",
            "Vegano", "https://letitv.com", "@letitv", null, null, null);

        Assert.Equal(DefaultId, establishment.Id);
        Assert.Equal(DefaultCategoryId, establishment.CategoryId);
        Assert.Equal("Let It V", establishment.Name);
        Assert.Equal("let-it-v", establishment.Slug);
        Assert.Equal("Vegano", establishment.Description);
        Assert.Equal("https://letitv.com", establishment.Website);
        Assert.Equal("@letitv", establishment.Instagram);
    }

    [Fact]
    public void Create_StartsActiveAndNotVerified()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);

        Assert.True(establishment.IsActive);
        Assert.False(establishment.IsVerified);
    }

    [Fact]
    public void Create_AcceptsNullOptionals()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);

        Assert.Null(establishment.Description);
        Assert.Null(establishment.Website);
        Assert.Null(establishment.Instagram);
    }

    [Fact]
    public void Create_NormalizesName_TrimsWhitespace()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "  Let It V  ", "let-it-v", null, null, null, null, null, null);

        Assert.Equal("Let It V", establishment.Name);
    }

    [Fact]
    public void Create_NormalizesName_CollapsesMultipleSpaces()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Let   It   V", "let-it-v", null, null, null, null, null, null);

        Assert.Equal("Let It V", establishment.Name);
    }

    [Fact]
    public void Create_NormalizesSlug_ToLowerCase()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "MY-SLUG", null, null, null, null, null, null);

        Assert.Equal("my-slug", establishment.Slug);
    }

    [Fact]
    public void Create_NormalizesSlug_TrimsWhitespace()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "  my-slug  ", null, null, null, null, null, null);

        Assert.Equal("my-slug", establishment.Slug);
    }

    [Fact]
    public void Create_NormalizesEmptyDescription_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", "   ", null, null, null, null, null);

        Assert.Null(establishment.Description);
    }

    [Fact]
    public void Create_NormalizesEmptyWebsite_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, "   ", null, null, null, null);

        Assert.Null(establishment.Website);
    }

    [Fact]
    public void Create_NormalizesEmptyInstagram_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, "   ", null, null, null);

        Assert.Null(establishment.Instagram);
    }

    [Fact]
    public void Create_TrimsDescription()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", "  Desc  ", null, null, null, null, null);

        Assert.Equal("Desc", establishment.Description);
    }

    [Fact]
    public void Create_TrimsWebsite()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, "  https://x.com  ", null, null, null, null);

        Assert.Equal("https://x.com", establishment.Website);
    }

    [Fact]
    public void Create_TrimsInstagram()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, "  @test  ", null, null, null);

        Assert.Equal("@test", establishment.Instagram);
    }

    [Fact]
    public void Create_RejectsEmptyCategoryId()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, new EstablishmentCategoryId(Guid.Empty),
                "Test", "test", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsEmptyName()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "", "test", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsWhitespaceOnlyName()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "   ", "test", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsTooShortName()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "A", "test", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        string longName = new('A', Establishment.NameMaxLength + 1);

        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, longName, "test", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsEmptySlug()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsWhitespaceOnlySlug()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "   ", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsTooShortSlug()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "a", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsTooLongSlug()
    {
        string longSlug = new string('a', Establishment.SlugMaxLength + 1);

        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", longSlug, null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsSlugWithSpaces()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "my slug", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsSlugStartingWithDash()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "-my-slug", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsSlugEndingWithDash()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "my-slug-", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsSlugWithConsecutiveDashes()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "my--slug", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsSlugWithSpecialCharacters()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "my_slug", null, null, null, null, null, null));
    }

    [Fact]
    public void Create_AcceptsValidSlugWithDashes()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "my-valid-slug", null, null, null, null, null, null);

        Assert.Equal("my-valid-slug", establishment.Slug);
    }

    [Fact]
    public void Create_AcceptsSlugWithNumbers()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "cafe2go", null, null, null, null, null, null);

        Assert.Equal("cafe2go", establishment.Slug);
    }

    [Fact]
    public void Create_RejectsTooLongDescription()
    {
        string longDesc = new('A', Establishment.DescriptionMaxLength + 1);

        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", longDesc, null, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsTooLongWebsite()
    {
        string longWebsite = "https://" + new string('a', Establishment.WebsiteMaxLength);

        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, longWebsite, null, null, null, null));
    }

    [Fact]
    public void Create_RejectsInvalidWebsiteUrl()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, "not-a-url", null, null, null, null));
    }

    [Fact]
    public void Create_RejectsWebsiteWithFtpScheme()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, "ftp://files.com", null, null, null, null));
    }

    [Fact]
    public void Create_AcceptsHttpWebsite()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, "http://example.com", null, null, null, null);

        Assert.Equal("http://example.com", establishment.Website);
    }

    [Fact]
    public void Create_AcceptsHttpsWebsite()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, "https://example.com", null, null, null, null);

        Assert.Equal("https://example.com", establishment.Website);
    }

    [Fact]
    public void Create_RejectsTooLongInstagram()
    {
        string longInsta = new('a', Establishment.InstagramMaxLength + 1);

        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, longInsta, null, null, null));
    }

    [Fact]
    public void Create_AcceptsNullLogoUrl()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);
        Assert.Null(establishment.LogoUrl);
    }

    [Fact]
    public void Create_AcceptsValidLogoUrl()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
            "https://cdn.example.com/logo.png", null, null);
        Assert.Equal("https://cdn.example.com/logo.png", establishment.LogoUrl);
    }

    [Fact]
    public void Create_RejectsInvalidLogoUrl()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
                "not-a-url", null, null));
    }

    [Fact]
    public void Create_RejectsTooLongLogoUrl()
    {
        string longUrl = "https://example.com/" + new string('a', Establishment.LogoUrlMaxLength);
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
                longUrl, null, null));
    }

    [Fact]
    public void Create_AcceptsNullContactEmail()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);
        Assert.Null(establishment.ContactEmail);
    }

    [Fact]
    public void Create_AcceptsValidContactEmail()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
            null, "info@example.com", null);
        Assert.Equal("info@example.com", establishment.ContactEmail);
    }

    [Fact]
    public void Create_RejectsInvalidContactEmail()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
                null, "not-an-email", null));
    }

    [Fact]
    public void Create_AcceptsNullContactPhone()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);
        Assert.Null(establishment.ContactPhone);
    }

    [Fact]
    public void Create_AcceptsValidContactPhone()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
            null, null, "+57 (300) 123-4567");
        Assert.Equal("+57 (300) 123-4567", establishment.ContactPhone);
    }

    [Fact]
    public void Create_RejectsTooLongContactPhone()
    {
        string longPhone = new string('1', Establishment.ContactPhoneMaxLength + 1);
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
                null, null, longPhone));
    }

    [Fact]
    public void Create_RejectsContactPhoneWithInvalidCharacters()
    {
        Assert.Throws<EstablishmentException>(() =>
            Establishment.Create(
                DefaultId, DefaultCategoryId, "Test", "test", null, null, null,
                null, null, "abc123"));
    }

    [Fact]
    public void Create_HasNullVerifiedAtUtc()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, null);
        Assert.Null(establishment.VerifiedAtUtc);
    }

    [Fact]
    public void Create_NormalizesEmptyLogoUrl_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, "  ", null, null);
        Assert.Null(establishment.LogoUrl);
    }

    [Fact]
    public void Create_NormalizesEmptyContactEmail_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, "  ", null);
        Assert.Null(establishment.ContactEmail);
    }

    [Fact]
    public void Create_NormalizesEmptyContactPhone_ToNull()
    {
        var establishment = Establishment.Create(
            DefaultId, DefaultCategoryId, "Test", "test", null, null, null, null, null, "  ");
        Assert.Null(establishment.ContactPhone);
    }
}
