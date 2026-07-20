namespace Lyria.Application.Features.Establishments;

public sealed record EstablishmentListFilter(
    string? Search,
    Guid? CategoryId,
    bool? IsActive,
    bool? IsVerified,
    int Page,
    int PageSize);
