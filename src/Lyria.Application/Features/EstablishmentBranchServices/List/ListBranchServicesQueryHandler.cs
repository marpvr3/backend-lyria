using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchServices.List;

public sealed class ListBranchServicesQueryHandler(
    IEstablishmentBranchServiceReadService readService)
    : IQueryHandler<ListBranchServicesQuery, PagedResponse<EstablishmentBranchServiceListItemResponse>>
{
    public async ValueTask<PagedResponse<EstablishmentBranchServiceListItemResponse>> Handle(
        ListBranchServicesQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        var filter = new EstablishmentBranchServiceListFilter(
            branchId, query.Search, query.IsActive, query.IsAvailable, query.Page, query.PageSize);

        return await readService.ListByBranchAsync(filter, cancellationToken);
    }
}
