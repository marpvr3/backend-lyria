using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Roles;

public sealed class RoleException : DomainException
{
    public RoleException(string message)
        : base(message)
    {
    }

    public RoleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
