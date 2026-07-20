using Lyria.Domain.Abstractions;
using Lyria.Domain.Services;
using Xunit;

namespace Lyria.Domain.UnitTests.Services;

public sealed class ServiceIdTests
{
    [Fact]
    public void Constructor_StoresValue()
    {
        var guid = Guid.NewGuid();
        var id = new ServiceId(guid);
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new ServiceId(guid);
        var id2 = new ServiceId(guid);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var id1 = ServiceId.New();
        var id2 = ServiceId.New();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void New_GeneratesNonEmptyGuid()
    {
        var id = ServiceId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void Implements_IStronglyTypedId()
    {
        var id = ServiceId.New();
        Assert.IsAssignableFrom<IStronglyTypedId<Guid>>(id);
    }
}
