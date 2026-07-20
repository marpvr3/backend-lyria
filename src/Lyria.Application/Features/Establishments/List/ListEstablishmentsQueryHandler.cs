using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Establishments.List;

public sealed class ListEstablishmentsQueryHandler(
    IEstablishmentReadService readService)
    : IQueryHandler<ListEstablishmentsQuery, PagedResponse<EstablishmentListItemResponse>>
{
    public async ValueTask<PagedResponse<EstablishmentListItemResponse>> Handle(
        ListEstablishmentsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new EstablishmentListFilter(
            query.Search,
            query.CategoryId,
            query.IsActive,
            query.IsVerified,
            query.Page,
            query.PageSize);

        return await readService.ListAsync(filter, cancellationToken);
    }
}
