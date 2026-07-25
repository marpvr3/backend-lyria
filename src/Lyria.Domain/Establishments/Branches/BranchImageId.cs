using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public readonly record struct BranchImageId(Guid Value) : IStronglyTypedId<Guid>
{
    public static BranchImageId New() => new(Guid.NewGuid());
}
