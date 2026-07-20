using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Restrictions;

public sealed class RestrictionException : DomainException
{
    public RestrictionException(string message)
        : base(message)
    {
    }

    public RestrictionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
