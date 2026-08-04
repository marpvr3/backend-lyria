namespace Lyria.Application.Features.Roles;

public sealed record RoleListFilter(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);
