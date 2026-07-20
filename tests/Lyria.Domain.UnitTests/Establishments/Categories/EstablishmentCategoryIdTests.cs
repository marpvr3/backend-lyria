using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments.Categories;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments.Categories;

public sealed class EstablishmentCategoryIdTests
{
    [Fact]
    public void Constructor_PreservesGuidValue()
    {
        var guid = Guid.NewGuid();
        var id = new EstablishmentCategoryId(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = new EstablishmentCategoryId(guid);
        var b = new EstablishmentCategoryId(guid);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        var a = new EstablishmentCategoryId(Guid.NewGuid());
        var b = new EstablishmentCategoryId(Guid.NewGuid());

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void New_ProducesNonEmptyGuid()
    {
        var id = EstablishmentCategoryId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        var id = new EstablishmentCategoryId(Guid.NewGuid());

        Assert.IsAssignableFrom<IStronglyTypedId<Guid>>(id);
    }
}
