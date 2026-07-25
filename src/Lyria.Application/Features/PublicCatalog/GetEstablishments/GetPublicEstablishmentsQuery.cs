using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishments;

public sealed record GetPublicEstablishmentsQuery(
    string? Search,
    Guid? CategoryId,
    string? City,
    string? Province,
    string? Country,
    Guid? ServiceId,
    Guid? RestrictionId,
    int? ComplianceLevel,
    bool? IsCertified,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "name",
    string SortDirection = "asc")
    : IQuery<Result<PagedResponse<PublicEstablishmentListItemResponse>>>;
