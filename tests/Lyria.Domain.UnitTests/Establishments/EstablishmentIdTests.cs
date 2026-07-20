using Lyria.Domain.Abstractions;
using Lyria.Domain.Establishments;
using Xunit;

namespace Lyria.Domain.UnitTests.Establishments;

public sealed class EstablishmentIdTests
{
    [Fact]
    public void Constructor_PreservesGuidValue()
    {
        var guid = Guid.NewGuid();
        var id = new EstablishmentId(guid);
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = new EstablishmentId(guid);
        var b = new EstablishmentId(guid);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        var a = new EstablishmentId(Guid.NewGuid());
        var b = new EstablishmentId(Guid.NewGuid());
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void New_ProducesNonEmptyGuid()
    {
        var id = EstablishmentId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        var id = new EstablishmentId(Guid.NewGuid());
        Assert.IsAssignableFrom<IStronglyTypedId<Guid>>(id);
    }
}
