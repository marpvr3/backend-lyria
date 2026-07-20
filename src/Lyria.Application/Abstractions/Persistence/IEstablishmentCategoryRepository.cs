using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentCategoryRepository
{
    Task<EstablishmentCategory?> GetByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string normalizedName,
        EstablishmentCategoryId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        EstablishmentCategory category,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
