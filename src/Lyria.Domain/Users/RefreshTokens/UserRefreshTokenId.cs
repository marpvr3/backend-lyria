using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users.RefreshTokens;

public readonly record struct UserRefreshTokenId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserRefreshTokenId New() => new(Guid.NewGuid());
}
