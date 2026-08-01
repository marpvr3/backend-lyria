using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Roles.List;

public sealed record GetRolesQuery(
    string? Search,
    string? Code,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null)
    : IQuery<PagedResponse<RoleListItemResponse>>;
