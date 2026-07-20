namespace Lyria.Application.Features.Services;

public sealed record ServiceListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    bool IsActive);
