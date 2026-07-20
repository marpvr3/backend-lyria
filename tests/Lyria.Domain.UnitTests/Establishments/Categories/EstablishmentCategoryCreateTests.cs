using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Categories;

public sealed class EstablishmentCategoryCreateTests
{
    [Fact]
    public void Create_WithValidData_CreatesCategory()
    {
        var id = EstablishmentCategoryId.New();

        var category = EstablishmentCategory.Create(id, "Restaurante", "Lugar para comer", null, 1);

        Assert.Equal(id, category.Id);
        Assert.Equal("Restaurante", category.Name);
        Assert.Equal("Lugar para comer", category.Description);
        Assert.Equal(1, category.SortOrder);
    }

    [Fact]
    public void Create_NormalizesName_ReducesMultipleSpaces()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Casa   de   Té", null, null, 0);

        Assert.Equal("Casa de Té", category.Name);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "  Cafetería  ", null, null, 0);

        Assert.Equal("Cafetería", category.Name);
    }

    [Fact]
    public void Create_NormalizesEmptyDescription_ToNull()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", "   ", null, 0);

        Assert.Null(category.Description);
    }

    [Fact]
    public void Create_NormalizesEmptyStringDescription_ToNull()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", "", null, 0);

        Assert.Null(category.Description);
    }

    [Fact]
    public void Create_StartsActive()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);

        Assert.True(category.IsActive);
    }

    [Fact]
    public void Create_PreservesSortOrder()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 42);

        Assert.Equal(42, category.SortOrder);
    }

    [Fact]
    public void Create_RejectsEmptyName()
    {
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "", null, null, 0));
    }

    [Fact]
    public void Create_RejectsWhitespaceOnlyName()
    {
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "   ", null, null, 0));
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        string longName = new('A', EstablishmentCategory.NameMaxLength + 1);

        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), longName, null, null, 0));
    }

    [Fact]
    public void Create_RejectsTooLongDescription()
    {
        string longDesc = new('A', EstablishmentCategory.DescriptionMaxLength + 1);

        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "Cafetería", longDesc, null, 0));
    }

    [Fact]
    public void Create_RejectsNegativeSortOrder()
    {
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "Cafetería", null, null, -1));
    }

    [Fact]
    public void Create_AcceptsZeroSortOrder()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);

        Assert.Equal(0, category.SortOrder);
    }

    [Fact]
    public void Create_PreservesNameCasingAndAccents()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería Artesanal", null, null, 0);

        Assert.Equal("Cafetería Artesanal", category.Name);
    }

    [Fact]
    public void Create_AcceptsNullIconUrl()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);
        Assert.Null(category.IconUrl);
    }

    [Fact]
    public void Create_AcceptsValidHttpIconUrl()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, "https://cdn.example.com/icon.png", 0);
        Assert.Equal("https://cdn.example.com/icon.png", category.IconUrl);
    }

    [Fact]
    public void Create_AcceptsValidHttpIconUrl_Http()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, "http://cdn.example.com/icon.png", 0);
        Assert.Equal("http://cdn.example.com/icon.png", category.IconUrl);
    }

    [Fact]
    public void Create_RejectsInvalidIconUrl()
    {
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "Cafetería", null, "not-a-url", 0));
    }

    [Fact]
    public void Create_RejectsTooLongIconUrl()
    {
        string longUrl = "https://example.com/" + new string('a', EstablishmentCategory.IconUrlMaxLength);
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "Cafetería", null, longUrl, 0));
    }

    [Fact]
    public void Create_RejectsIconUrlWithFtpScheme()
    {
        Assert.Throws<EstablishmentCategoryException>(() =>
            EstablishmentCategory.Create(
                EstablishmentCategoryId.New(), "Cafetería", null, "ftp://files.example.com/icon.png", 0));
    }

    [Fact]
    public void Create_NormalizesEmptyIconUrl_ToNull()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, "   ", 0);
        Assert.Null(category.IconUrl);
    }

    [Fact]
    public void Create_TrimsIconUrl()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, "  https://cdn.example.com/icon.png  ", 0);
        Assert.Equal("https://cdn.example.com/icon.png", category.IconUrl);
    }

    [Fact]
    public void Create_DoesNotExposeCodeProperty()
    {
        var category = EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Cafetería", null, null, 0);

        var properties = typeof(EstablishmentCategory).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name == "Code");
    }
}
