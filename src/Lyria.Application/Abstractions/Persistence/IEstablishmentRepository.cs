using Lyria.Domain.Establishments;

namespace Lyria.Application.Abstractions.Persistence;

public interface IEstablishmentRepository
{
    Task<Establishment?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsBySlugAsync(
        string normalizedSlug,
        EstablishmentId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Establishment establishment,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
