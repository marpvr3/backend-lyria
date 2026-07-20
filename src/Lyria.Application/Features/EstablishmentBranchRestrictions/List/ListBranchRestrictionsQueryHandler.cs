using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.List;

public sealed class ListBranchRestrictionsQueryHandler(
    IEstablishmentBranchRestrictionReadService readService)
    : IQueryHandler<ListBranchRestrictionsQuery, PagedResponse<EstablishmentBranchRestrictionListItemResponse>>
{
    public async ValueTask<PagedResponse<EstablishmentBranchRestrictionListItemResponse>> Handle(
        ListBranchRestrictionsQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        var filter = new EstablishmentBranchRestrictionListFilter(
            branchId, query.Search, query.IsActive, query.ComplianceLevel,
            query.IsCertified, query.Page, query.PageSize);

        return await readService.ListByBranchAsync(filter, cancellationToken);
    }
}
