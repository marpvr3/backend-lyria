using Lyria.Domain.Abstractions;
using Xunit;

namespace Lyria.Domain.UnitTests.Abstractions;

public sealed class AggregateRootDomainEventTests
{
    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_AddsEventToCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.ApplyTestEvent();

        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<TestDomainEvent>(aggregate.DomainEvents.First());
    }

    [Fact]
    public void RaiseDomainEvent_MultipleTimes_AddsAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.ApplyTestEvent();
        aggregate.ApplyTestEvent();
        aggregate.ApplyTestEvent();

        Assert.Equal(3, aggregate.DomainEvents.Count);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.ApplyTestEvent();
        aggregate.ApplyTestEvent();

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_NullEvent_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => aggregate.ApplyNullEvent());
    }

    [Fact]
    public void DomainEvents_ReturnsReadOnlyCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.ApplyTestEvent();

        var events = aggregate.DomainEvents;

        Assert.IsAssignableFrom<IReadOnlyCollection<IDomainEvent>>(events);
    }

    [Fact]
    public void ClearDomainEvents_OnEmptyCollection_DoesNotThrow()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        var exception = Record.Exception(() => aggregate.ClearDomainEvents());

        Assert.Null(exception);
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void ApplyTestEvent()
        {
            RaiseDomainEvent(new TestDomainEvent());
        }

        public void ApplyNullEvent()
        {
            RaiseDomainEvent(null!);
        }
    }

    private sealed class TestDomainEvent : IDomainEvent;
}
