using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Establishments;

public sealed class EstablishmentException : DomainException
{
    public EstablishmentException(string message)
        : base(message)
    {
    }

    public EstablishmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
