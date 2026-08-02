using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Users.UserRestrictions;

public sealed class UserRestrictionException : DomainException
{
    public UserRestrictionException(string message)
        : base(message)
    {
    }

    public UserRestrictionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
