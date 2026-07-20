using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments;

public readonly record struct EstablishmentId(Guid Value) : IStronglyTypedId<Guid>
{
    public static EstablishmentId New() => new(Guid.NewGuid());
}
