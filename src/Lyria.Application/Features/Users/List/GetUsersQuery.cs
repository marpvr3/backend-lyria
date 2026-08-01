using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Users.List;

public sealed record GetUsersQuery(
    string? Search,
    string? Email,
    string? Status,
    bool? EmailVerified,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null)
    : IQuery<PagedResponse<UserListItemResponse>>;
