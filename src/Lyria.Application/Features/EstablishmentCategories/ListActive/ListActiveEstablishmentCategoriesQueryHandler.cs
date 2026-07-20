using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;

namespace Lyria.Application.Features.EstablishmentCategories.ListActive;

public sealed class ListActiveEstablishmentCategoriesQueryHandler(
    IEstablishmentCategoryReadService readService)
    : IQueryHandler<ListActiveEstablishmentCategoriesQuery, IReadOnlyList<EstablishmentCategoryResponse>>
{
    public async ValueTask<IReadOnlyList<EstablishmentCategoryResponse>> Handle(
        ListActiveEstablishmentCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        return await readService.ListActiveAsync(cancellationToken);
    }
}
