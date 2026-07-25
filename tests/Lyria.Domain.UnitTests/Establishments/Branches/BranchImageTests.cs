using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Branches;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Branches;

public sealed class BranchImageTests
{
    private static readonly EstablishmentBranchId BranchId = EstablishmentBranchId.New();

    private static BranchImage CreateValidImage(
        bool isPrimary = false,
        int sortOrder = 0,
        string url = "https://cdn.example.com/img.jpg",
        string fileName = "img.jpg",
        string? alternativeText = null)
    {
        return BranchImage.Create(
            BranchImageId.New(),
            BranchId,
            url,
            fileName,
            alternativeText,
            isPrimary,
            sortOrder);
    }

    [Fact]
    public void Create_ValidImage_SetsAllProperties()
    {
        var image = BranchImage.Create(
            BranchImageId.New(), BranchId,
            "https://cdn.example.com/fachada.jpg",
            "fachada.jpg",
            "Fachada principal",
            true,
            0);

        Assert.Equal(BranchId, image.BranchId);
        Assert.Equal("https://cdn.example.com/fachada.jpg", image.Url);
        Assert.Equal("fachada.jpg", image.FileName);
        Assert.Equal("Fachada principal", image.AlternativeText);
        Assert.True(image.IsPrimary);
        Assert.Equal(0, image.SortOrder);
        Assert.True(image.IsActive);
    }

    [Fact]
    public void Create_NormalizesUrl()
    {
        var image = CreateValidImage(url: "  https://cdn.example.com/img.jpg  ");

        Assert.Equal("https://cdn.example.com/img.jpg", image.Url);
    }

    [Fact]
    public void Create_NormalizesFileName()
    {
        var image = CreateValidImage(fileName: "  fachada.jpg  ");

        Assert.Equal("fachada.jpg", image.FileName);
    }

    [Fact]
    public void Create_NormalizesAlternativeText()
    {
        var image = CreateValidImage(alternativeText: "  Texto alternativo  ");

        Assert.Equal("Texto alternativo", image.AlternativeText);
    }

    [Fact]
    public void Create_NullAlternativeText_SetsNull()
    {
        var image = CreateValidImage(alternativeText: null);

        Assert.Null(image.AlternativeText);
    }

    [Fact]
    public void Create_WhitespaceAlternativeText_SetsNull()
    {
        var image = CreateValidImage(alternativeText: "   ");

        Assert.Null(image.AlternativeText);
    }

    [Fact]
    public void Create_EmptyUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: ""));
    }

    [Fact]
    public void Create_WhitespaceUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "   "));
    }

    [Fact]
    public void Create_JavascriptSchemeUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "javascript:alert(1)"));
    }

    [Fact]
    public void Create_DataSchemeUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "data:text/html,<h1>test</h1>"));
    }

    [Fact]
    public void Create_VbscriptSchemeUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "vbscript:msgbox"));
    }

    [Fact]
    public void Create_FileSchemeUrl_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "file:///etc/passwd"));
    }

    [Fact]
    public void Create_HttpUrl_DoesNotThrow()
    {
        var image = CreateValidImage(url: "http://example.com/img.jpg");

        Assert.Equal("http://example.com/img.jpg", image.Url);
    }

    [Fact]
    public void Create_RelativeUrl_DoesNotThrow()
    {
        var image = CreateValidImage(url: "/images/fachada.jpg");

        Assert.Equal("/images/fachada.jpg", image.Url);
    }

    [Fact]
    public void Create_UrlExceedsMaxLength_Throws()
    {
        string longUrl = "https://cdn.example.com/" + new string('a', BranchImage.UrlMaxLength);

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: longUrl));
    }

    [Fact]
    public void Create_EmptyFileName_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: ""));
    }

    [Fact]
    public void Create_WhitespaceFileName_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: "   "));
    }

    [Fact]
    public void Create_FileNameWithPathTraversal_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: "../etc/passwd"));
    }

    [Fact]
    public void Create_FileNameWithBackslashPathTraversal_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: "..\\windows\\system32"));
    }

    [Fact]
    public void Create_FileNameWithForwardSlash_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: "path/image.jpg"));
    }

    [Fact]
    public void Create_FileNameWithBackslash_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: "path\\image.jpg"));
    }

    [Fact]
    public void Create_FileNameExceedsMaxLength_Throws()
    {
        string longName = new string('a', BranchImage.FileNameMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(fileName: longName));
    }

    [Fact]
    public void Create_NegativeSortOrder_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(sortOrder: -1));
    }

    [Fact]
    public void Create_ZeroSortOrder_DoesNotThrow()
    {
        var image = CreateValidImage(sortOrder: 0);

        Assert.Equal(0, image.SortOrder);
    }

    [Fact]
    public void Create_EmptyBranchId_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            BranchImage.Create(
                BranchImageId.New(),
                new EstablishmentBranchId(Guid.Empty),
                "https://cdn.example.com/img.jpg",
                "img.jpg",
                null,
                false,
                0));
    }

    [Fact]
    public void UpdateMetadata_SetsNewValues()
    {
        var image = CreateValidImage();

        image.UpdateMetadata(
            "https://cdn.example.com/new.jpg",
            "new.jpg",
            "Nuevo texto",
            5);

        Assert.Equal("https://cdn.example.com/new.jpg", image.Url);
        Assert.Equal("new.jpg", image.FileName);
        Assert.Equal("Nuevo texto", image.AlternativeText);
        Assert.Equal(5, image.SortOrder);
    }

    [Fact]
    public void SetAsPrimary_ActiveImage_SetsIsPrimaryTrue()
    {
        var image = CreateValidImage(isPrimary: false);

        image.SetAsPrimary();

        Assert.True(image.IsPrimary);
    }

    [Fact]
    public void SetAsPrimary_InactiveImage_Throws()
    {
        var image = CreateValidImage(isPrimary: false);
        image.Deactivate();

        Assert.Throws<EstablishmentBranchException>(() =>
            image.SetAsPrimary());
    }

    [Fact]
    public void UnsetPrimary_SetsIsPrimaryFalse()
    {
        var image = CreateValidImage(isPrimary: true);

        image.UnsetPrimary();

        Assert.False(image.IsPrimary);
    }

    [Fact]
    public void Deactivate_PrimaryImage_UnsetsIsPrimary()
    {
        var image = CreateValidImage(isPrimary: true);

        image.Deactivate();

        Assert.False(image.IsActive);
        Assert.False(image.IsPrimary);
    }

    [Fact]
    public void Deactivate_NonPrimaryImage_SetsInactive()
    {
        var image = CreateValidImage(isPrimary: false);

        image.Deactivate();

        Assert.False(image.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var image = CreateValidImage();
        image.Deactivate();

        image.Activate();

        Assert.True(image.IsActive);
    }

    [Fact]
    public void NoPublicSetters()
    {
        var properties = typeof(BranchImage).GetProperties();
        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod(nonPublic: false);
            Assert.Null(setter);
        }
    }

    [Fact]
    public void ImplementsIAuditableEntity()
    {
        Assert.True(typeof(IAuditableEntity).IsAssignableFrom(typeof(BranchImage)));
    }

    [Fact]
    public void Create_DangerousSchemeUpperCase_Throws()
    {
        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(url: "JAVASCRIPT:alert(1)"));
    }

    [Fact]
    public void AlternativeTextExceedsMaxLength_Throws()
    {
        string longText = new string('a', BranchImage.AlternativeTextMaxLength + 1);

        Assert.Throws<EstablishmentBranchException>(() =>
            CreateValidImage(alternativeText: longText));
    }
}
