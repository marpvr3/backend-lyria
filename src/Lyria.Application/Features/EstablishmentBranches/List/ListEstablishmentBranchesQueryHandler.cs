using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.EstablishmentBranches.List;

public sealed class ListEstablishmentBranchesQueryHandler(
    IEstablishmentBranchReadService readService,
    IEstablishmentReadService establishmentReadService)
    : IQueryHandler<ListEstablishmentBranchesQuery, PagedResponse<EstablishmentBranchListItemResponse>>
{
    public async ValueTask<PagedResponse<EstablishmentBranchListItemResponse>> Handle(
        ListEstablishmentBranchesQuery query,
        CancellationToken cancellationToken)
    {
        var establishmentId = new EstablishmentId(query.EstablishmentId);

        var establishment = await establishmentReadService.GetByIdAsync(
            establishmentId, cancellationToken);

        if (establishment is null)
        {
            return new PagedResponse<EstablishmentBranchListItemResponse>(
                [], query.Page, query.PageSize, 0);
        }

        var filter = new EstablishmentBranchListFilter(
            query.EstablishmentId,
            query.Search,
            query.IsActive,
            query.Page,
            query.PageSize);

        return await readService.ListByEstablishmentAsync(filter, cancellationToken);
    }
}
