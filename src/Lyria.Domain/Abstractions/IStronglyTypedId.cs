namespace Lyria.Domain.Abstractions;

public interface IStronglyTypedId<out TValue>
    where TValue : notnull
{
    TValue Value { get; }
}
