using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentCategoryReadService
{
    Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
        CancellationToken cancellationToken);
}
