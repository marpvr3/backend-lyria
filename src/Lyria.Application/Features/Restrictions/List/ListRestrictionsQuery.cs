using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Restrictions.List;

public sealed record ListRestrictionsQuery(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<RestrictionListItemResponse>>;
