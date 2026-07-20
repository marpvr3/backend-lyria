using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Restrictions;

public readonly record struct RestrictionId(Guid Value) : IStronglyTypedId<Guid>
{
    public static RestrictionId New() => new(Guid.NewGuid());
}
