using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Establishments.Branches;

public sealed class EstablishmentBranchException : DomainException
{
    public EstablishmentBranchException(string message)
        : base(message)
    {
    }

    public EstablishmentBranchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
