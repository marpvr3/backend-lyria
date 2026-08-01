using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Users.List;

public sealed class GetUsersQueryHandler(
    IUserReadService readService)
    : IQueryHandler<GetUsersQuery, PagedResponse<UserListItemResponse>>
{
    public async ValueTask<PagedResponse<UserListItemResponse>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new UserListFilter(
            query.Search,
            query.Email,
            query.Status,
            query.EmailVerified,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortDirection);

        return await readService.ListAsync(filter, cancellationToken);
    }
}
