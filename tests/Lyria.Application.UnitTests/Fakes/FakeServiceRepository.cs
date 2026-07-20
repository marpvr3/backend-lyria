using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeServiceRepository : IServiceRepository
{
    private readonly List<Service> _services = [];

    public int SaveChangesCallCount { get; private set; }

    public void Seed(Service service) => _services.Add(service);

    public Task<Service?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_services.FirstOrDefault(s => s.Id == id));
    }

    public Task<bool> ExistsByNameAsync(
        string normalizedName,
        ServiceId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _services.Any(s =>
            string.Equals(s.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            (excludingId is null || s.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Service service,
        CancellationToken cancellationToken)
    {
        _services.Add(service);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
