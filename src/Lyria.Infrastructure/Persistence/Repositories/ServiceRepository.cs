using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRepository(LyriaDbContext dbContext)
    : IServiceRepository
{
    public async Task<Service?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Service>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string normalizedName,
        ServiceId? excludingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Service>()
            .AnyAsync(
                s => s.Name == normalizedName &&
                     (excludingId == null || s.Id != excludingId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Service service,
        CancellationToken cancellationToken)
    {
        await dbContext.Set<Service>()
            .AddAsync(service, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
