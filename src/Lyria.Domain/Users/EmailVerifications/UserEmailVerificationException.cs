using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Users.EmailVerifications;

public sealed class UserEmailVerificationException : DomainException
{
    public UserEmailVerificationException(string message)
        : base(message)
    {
    }

    public UserEmailVerificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
