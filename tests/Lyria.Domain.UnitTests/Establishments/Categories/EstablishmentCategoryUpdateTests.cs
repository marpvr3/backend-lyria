using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Categories;

public sealed class EstablishmentCategoryUpdateTests
{
    [Fact]
    public void UpdateDetails_WithValidData_UpdatesAllFields()
    {
        var category = CreateCategory();

        category.UpdateDetails("Bar", "Un bar", null, 5);

        Assert.Equal("Bar", category.Name);
        Assert.Equal("Un bar", category.Description);
        Assert.Equal(5, category.SortOrder);
    }

    [Fact]
    public void UpdateDetails_PreservesId()
    {
        var category = CreateCategory();
        var originalId = category.Id;

        category.UpdateDetails("Bar", null, null, 0);

        Assert.Equal(originalId, category.Id);
    }

    [Fact]
    public void UpdateDetails_NormalizesName()
    {
        var category = CreateCategory();

        category.UpdateDetails("  Mi   Bar  ", null, null, 0);

        Assert.Equal("Mi Bar", category.Name);
    }

    [Fact]
    public void UpdateDetails_NormalizesEmptyDescriptionToNull()
    {
        var category = CreateCategory();

        category.UpdateDetails("Bar", "  ", null, 0);

        Assert.Null(category.Description);
    }

    [Fact]
    public void UpdateDetails_DoesNotModifyIsActive()
    {
        var category = CreateCategory();
        category.Deactivate();

        category.UpdateDetails("Bar", null, null, 0);

        Assert.False(category.IsActive);
    }

    [Fact]
    public void UpdateDetails_RejectsInvalidData()
    {
        var category = CreateCategory();

        Assert.Throws<EstablishmentCategoryException>(() =>
            category.UpdateDetails("", null, null, 0));
    }

    [Fact]
    public void ChangeSortOrder_UpdatesOrder()
    {
        var category = CreateCategory();

        category.ChangeSortOrder(99);

        Assert.Equal(99, category.SortOrder);
    }

    [Fact]
    public void ChangeSortOrder_RejectsNegative()
    {
        var category = CreateCategory();

        Assert.Throws<EstablishmentCategoryException>(() =>
            category.ChangeSortOrder(-1));
    }

    private static EstablishmentCategory CreateCategory() =>
        EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", "Descripción", null, 1);
}
