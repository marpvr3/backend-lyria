using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.EstablishmentBranches.List;

public sealed record ListEstablishmentBranchesQuery(
    Guid EstablishmentId,
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<EstablishmentBranchListItemResponse>>;
