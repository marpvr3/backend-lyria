using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Users;

public sealed class UserException : DomainException
{
    public UserException(string message)
        : base(message)
    {
    }

    public UserException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
