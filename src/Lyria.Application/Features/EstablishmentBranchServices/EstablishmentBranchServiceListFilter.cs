using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.EstablishmentBranchServices;

public sealed record EstablishmentBranchServiceListFilter(
    EstablishmentBranchId BranchId,
    string? Search,
    bool? IsActive,
    bool? IsAvailable,
    int Page,
    int PageSize);
