using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Services.List;

public sealed class ListServicesQueryHandler(
    IServiceReadService readService)
    : IQueryHandler<ListServicesQuery, PagedResponse<ServiceListItemResponse>>
{
    public async ValueTask<PagedResponse<ServiceListItemResponse>> Handle(
        ListServicesQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new ServiceListFilter(
            query.Search, query.IsActive, query.Page, query.PageSize);

        return await readService.ListAsync(filter, cancellationToken);
    }
}
