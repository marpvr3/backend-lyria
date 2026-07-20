using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Restrictions.List;

public sealed class ListRestrictionsQueryHandler(
    IRestrictionReadService readService)
    : IQueryHandler<ListRestrictionsQuery, PagedResponse<RestrictionListItemResponse>>
{
    public async ValueTask<PagedResponse<RestrictionListItemResponse>> Handle(
        ListRestrictionsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new RestrictionListFilter(
            query.Search, query.IsActive, query.Page, query.PageSize);

        return await readService.ListAsync(filter, cancellationToken);
    }
}
