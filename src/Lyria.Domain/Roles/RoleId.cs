using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Roles;

public readonly record struct RoleId(Guid Value) : IStronglyTypedId<Guid>
{
    public static RoleId New() => new(Guid.NewGuid());
}
