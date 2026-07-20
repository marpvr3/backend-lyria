namespace Lyria.Application.Features.Services;

public sealed record ServiceResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
