namespace Lyria.Application.Features.Roles;

public sealed record RoleListFilter(
    string? Search,
    string? Code,
    bool? IsActive,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);
