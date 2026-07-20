using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;

namespace Lyria.Application.Features.Establishments.List;

public sealed record ListEstablishmentsQuery(
    string? Search,
    Guid? CategoryId,
    bool? IsActive,
    bool? IsVerified,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<EstablishmentListItemResponse>>;
