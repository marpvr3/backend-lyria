using Lyria.Domain.Abstractions;
using Lyria.Domain.Restrictions;
using Xunit;

namespace Lyria.Domain.UnitTests.Restrictions;

public sealed class RestrictionIdTests
{
    [Fact]
    public void Constructor_PreservesValue()
    {
        Guid value = Guid.NewGuid();
        var id = new RestrictionId(value);
        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Guid value = Guid.NewGuid();
        var id1 = new RestrictionId(value);
        var id2 = new RestrictionId(value);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id1 = new RestrictionId(Guid.NewGuid());
        var id2 = new RestrictionId(Guid.NewGuid());
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        var id = RestrictionId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void ImplementsStronglyTypedId()
    {
        var id = RestrictionId.New();
        Assert.IsAssignableFrom<IStronglyTypedId<Guid>>(id);
    }
}
