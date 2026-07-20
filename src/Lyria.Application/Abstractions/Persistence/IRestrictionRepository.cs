using Lyria.Domain.Restrictions;

namespace Lyria.Application.Abstractions.Persistence;

public interface IRestrictionRepository
{
    Task<Restriction?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string normalizedName,
        RestrictionId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Restriction restriction,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
