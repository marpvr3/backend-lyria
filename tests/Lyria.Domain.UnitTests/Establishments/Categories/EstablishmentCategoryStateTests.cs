using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Categories;

public sealed class EstablishmentCategoryStateTests
{
    [Fact]
    public void Deactivate_ChangesToInactive()
    {
        var category = CreateCategory();

        category.Deactivate();

        Assert.False(category.IsActive);
    }

    [Fact]
    public void Deactivate_IsIdempotent()
    {
        var category = CreateCategory();

        category.Deactivate();
        category.Deactivate();

        Assert.False(category.IsActive);
    }

    [Fact]
    public void Reactivate_ChangesToActive()
    {
        var category = CreateCategory();
        category.Deactivate();

        category.Reactivate();

        Assert.True(category.IsActive);
    }

    [Fact]
    public void Reactivate_IsIdempotent()
    {
        var category = CreateCategory();

        category.Reactivate();
        category.Reactivate();

        Assert.True(category.IsActive);
    }

    private static EstablishmentCategory CreateCategory() =>
        EstablishmentCategory.Create(
            EstablishmentCategoryId.New(), "Restaurante", null, null, 0);
}
