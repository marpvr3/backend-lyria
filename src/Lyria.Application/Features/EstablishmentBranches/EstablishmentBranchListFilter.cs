namespace Lyria.Application.Features.EstablishmentBranches;

public sealed record EstablishmentBranchListFilter(
    Guid EstablishmentId,
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);
