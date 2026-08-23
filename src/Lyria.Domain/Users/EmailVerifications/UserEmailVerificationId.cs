using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Users.EmailVerifications;

public readonly record struct UserEmailVerificationId(Guid Value) : IStronglyTypedId<Guid>
{
    public static UserEmailVerificationId New() => new(Guid.NewGuid());
}
