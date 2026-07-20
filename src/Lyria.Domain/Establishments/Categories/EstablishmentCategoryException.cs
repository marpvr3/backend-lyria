using Lyria.Domain.Exceptions;

namespace Lyria.Domain.Establishments.Categories;

public sealed class EstablishmentCategoryException : DomainException
{
    public EstablishmentCategoryException(string message)
        : base(message)
    {
    }

    public EstablishmentCategoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
