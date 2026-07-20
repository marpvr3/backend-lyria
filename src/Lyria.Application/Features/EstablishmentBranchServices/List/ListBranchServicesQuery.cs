using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.EstablishmentBranchServices.List;

public sealed record ListBranchServicesQuery(
    Guid BranchId,
    string? Search,
    bool? IsActive,
    bool? IsAvailable,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<EstablishmentBranchServiceListItemResponse>>;
