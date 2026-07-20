using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Services;

public sealed class ServiceException : DomainException
{
    public ServiceException(string message)
        : base(message)
    {
    }

    public ServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
