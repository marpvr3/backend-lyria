using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentRepository : IEstablishmentRepository
{
    private readonly List<Establishment> _establishments = [];
    public IReadOnlyList<Establishment> Added => _establishments;
    public int SaveChangesCallCount { get; private set; }

    public Task<Establishment?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken)
    {
        Establishment? found = _establishments.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(found);
    }

    public Task<bool> ExistsBySlugAsync(
        string normalizedSlug,
        EstablishmentId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _establishments.Any(e =>
            e.Slug == normalizedSlug &&
            (excludingId is null || e.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Establishment establishment,
        CancellationToken cancellationToken)
    {
        _establishments.Add(establishment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(Establishment establishment)
    {
        _establishments.Add(establishment);
    }
}
