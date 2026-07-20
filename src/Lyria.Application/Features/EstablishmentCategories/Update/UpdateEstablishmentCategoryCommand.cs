using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.EstablishmentCategories.Update;

public sealed record UpdateEstablishmentCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    int SortOrder) : ICommand;
