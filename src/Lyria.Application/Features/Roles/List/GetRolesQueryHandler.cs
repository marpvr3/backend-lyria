using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Roles.List;

public sealed class GetRolesQueryHandler(
    IRoleReadService readService)
    : IQueryHandler<GetRolesQuery, PagedResponse<RoleListItemResponse>>
{
    public async ValueTask<PagedResponse<RoleListItemResponse>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new RoleListFilter(
            query.Search,
            query.IsActive,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortDirection);

        return await readService.ListAsync(filter, cancellationToken);
    }
}
