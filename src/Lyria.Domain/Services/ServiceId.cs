using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Services;

public readonly record struct ServiceId(Guid Value) : IStronglyTypedId<Guid>
{
    public static ServiceId New() => new(Guid.NewGuid());
}
