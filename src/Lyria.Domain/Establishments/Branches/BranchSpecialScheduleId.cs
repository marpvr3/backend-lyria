using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public readonly record struct BranchSpecialScheduleId(Guid Value) : IStronglyTypedId<Guid>
{
    public static BranchSpecialScheduleId New() => new(Guid.NewGuid());
}
