using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Services.List;

public sealed record ListServicesQuery(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<ServiceListItemResponse>>;
