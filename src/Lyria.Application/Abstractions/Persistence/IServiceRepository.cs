using Lyria.Domain.Services;

namespace Lyria.Application.Abstractions.Persistence;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string normalizedName,
        ServiceId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Service service,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
