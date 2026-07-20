using Lyria.Application.Abstractions.Messaging;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Features.EstablishmentCategories.Create;

public sealed record CreateEstablishmentCategoryCommand(
    string Name,
    string? Description,
    string? IconUrl,
    int SortOrder) : ICommand<EstablishmentCategoryId>;
