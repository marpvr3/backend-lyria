using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions;

public sealed record EstablishmentBranchRestrictionListFilter(
    EstablishmentBranchId BranchId,
    string? Search,
    bool? IsActive,
    RestrictionComplianceLevel? ComplianceLevel,
    bool? IsCertified,
    int Page,
    int PageSize);
