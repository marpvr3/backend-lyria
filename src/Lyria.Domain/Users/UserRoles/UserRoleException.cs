using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Users.UserRoles;

public sealed class UserRoleException : DomainException
{
    public UserRoleException(string message)
        : base(message)
    {
    }

    public UserRoleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
