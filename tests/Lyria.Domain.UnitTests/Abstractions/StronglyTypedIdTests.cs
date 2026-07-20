using Lyria.Domain.Abstractions;
using Xunit;

namespace Lyria.Domain.UnitTests.Abstractions;

public sealed class StronglyTypedIdTests
{
    [Fact]
    public void Value_ReturnsAssignedValue()
    {
        var guid = Guid.NewGuid();
        var id = new TestId(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void RecordStruct_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();
        var a = new TestId(guid);
        var b = new TestId(guid);

        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordStruct_DifferentValue_AreNotEqual()
    {
        var a = new TestId(Guid.NewGuid());
        var b = new TestId(Guid.NewGuid());

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TestId_ImplementsIStronglyTypedId()
    {
        var id = new TestId(Guid.NewGuid());

        Assert.IsAssignableFrom<IStronglyTypedId<Guid>>(id);
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId<Guid>;
}
