using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Branches;

public readonly record struct EstablishmentBranchId(Guid Value) : IStronglyTypedId<Guid>
{
    public static EstablishmentBranchId New() => new(Guid.NewGuid());
}
