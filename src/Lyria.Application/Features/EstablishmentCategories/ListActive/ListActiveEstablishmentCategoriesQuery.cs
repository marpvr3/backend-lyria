using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentCategories.ListActive;

public sealed record ListActiveEstablishmentCategoriesQuery()
    : IQuery<IReadOnlyList<EstablishmentCategoryResponse>>;
