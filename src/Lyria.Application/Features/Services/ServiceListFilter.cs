namespace Lyria.Application.Features.Services;

public sealed record ServiceListFilter(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);
