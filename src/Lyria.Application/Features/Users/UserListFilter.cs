namespace Lyria.Application.Features.Users;

public sealed record UserListFilter(
    string? Search,
    string? Email,
    string? Status,
    bool? EmailVerified,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);
