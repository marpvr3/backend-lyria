using Lyria.Domain.Abstractions;

namespace Lyria.Domain.Establishments.Categories;

public readonly record struct EstablishmentCategoryId(Guid Value) : IStronglyTypedId<Guid>
{
    public static EstablishmentCategoryId New() => new(Guid.NewGuid());
}
