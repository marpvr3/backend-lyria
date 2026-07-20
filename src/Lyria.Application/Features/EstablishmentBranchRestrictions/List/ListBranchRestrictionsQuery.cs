using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.List;

public sealed record ListBranchRestrictionsQuery(
    Guid BranchId,
    string? Search,
    bool? IsActive,
    RestrictionComplianceLevel? ComplianceLevel,
    bool? IsCertified,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<EstablishmentBranchRestrictionListItemResponse>>;
