using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Users.RefreshTokens;

public sealed class UserRefreshTokenException : DomainException
{
    public UserRefreshTokenException(string message)
        : base(message)
    {
    }

    public UserRefreshTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
