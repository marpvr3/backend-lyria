using Lyria.Domain.Abstractions;
using Xunit;

namespace Lyria.Domain.UnitTests.ValueObjects;

public sealed class ValueObjectEqualityTests
{
    [Fact]
    public void Equals_WithSameComponents_ReturnsTrue()
    {
        var a = new TestValueObject("test", 1);
        var b = new TestValueObject("test", 1);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_WithDifferentComponents_ReturnsFalse()
    {
        var a = new TestValueObject("test", 1);
        var b = new TestValueObject("other", 2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_WithSameComponents_ReturnsSameHash()
    {
        var a = new TestValueObject("test", 1);
        var b = new TestValueObject("test", 1);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var a = new TestValueObject("test", 1);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void Equals_DifferentValueObjectType_ReturnsFalse()
    {
        var a = new TestValueObject("test", 1);
        var b = new OtherTestValueObject("test", 1);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Operator_EqualObjects_ReturnsTrue()
    {
        var a = new TestValueObject("test", 1);
        var b = new TestValueObject("test", 1);

        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Operator_NullComparisons_AreConsistent()
    {
        var a = new TestValueObject("test", 1);

        Assert.False(a == null);
        Assert.False(null == a);
        Assert.True((TestValueObject?)null == (TestValueObject?)null);
    }

    private sealed class TestValueObject(string value, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return value;
            yield return number;
        }
    }

    private sealed class OtherTestValueObject(string value, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return value;
            yield return number;
        }
    }
}
