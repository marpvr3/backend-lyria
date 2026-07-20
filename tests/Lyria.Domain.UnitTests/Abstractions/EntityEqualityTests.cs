using Lyria.Domain.Abstractions;
using Xunit;

namespace Lyria.Domain.UnitTests.Abstractions;

public sealed class EntityEqualityTests
{
    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Equals_DifferentEntityType_SameId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);
        var other = new OtherTestEntity(id);

        Assert.False(entity.Equals(other));
    }

    [Fact]
    public void Equals_SameTypeSameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_BothTransient_ReturnsFalse()
    {
        var a = new TestEntity(Guid.Empty);
        var b = new TestEntity(Guid.Empty);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_OneTransient_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.Empty);

        Assert.False(a.Equals(b));
        Assert.False(b.Equals(a));
    }

    [Fact]
    public void Operators_AreConsistentWithEquals()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);
        var c = new TestEntity(Guid.NewGuid());

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.False(a == null);
        Assert.False(null == a);
        Assert.True((TestEntity?)null == (TestEntity?)null);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherTestEntity(Guid id) : Entity<Guid>(id);
}
