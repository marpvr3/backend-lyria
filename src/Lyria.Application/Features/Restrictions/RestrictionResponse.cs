namespace Lyria.Application.Features.Restrictions;

public sealed record RestrictionResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
