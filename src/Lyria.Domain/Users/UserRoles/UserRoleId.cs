using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users.UserRoles;

public readonly record struct UserRoleId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserRoleId New() => new(Guid.NewGuid());
}
