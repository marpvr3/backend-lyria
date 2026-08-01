using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users;

public readonly record struct UserId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserId New() => new(Guid.NewGuid());
}
