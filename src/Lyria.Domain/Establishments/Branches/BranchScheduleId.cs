using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public readonly record struct BranchScheduleId(Guid Value) : IStronglyTypedId<Guid>
{
    public static BranchScheduleId New() => new(Guid.NewGuid());
}
