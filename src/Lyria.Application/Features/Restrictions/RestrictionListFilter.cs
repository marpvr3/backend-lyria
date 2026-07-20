namespace Lyria.Application.Features.Restrictions;

public sealed record RestrictionListFilter(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);
