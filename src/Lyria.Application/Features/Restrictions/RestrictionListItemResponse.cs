namespace Lyria.Application.Features.Restrictions;

public sealed record RestrictionListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
