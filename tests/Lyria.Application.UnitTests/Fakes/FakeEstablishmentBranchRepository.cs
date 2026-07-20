using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchRepository : IEstablishmentBranchRepository
{
    private readonly List<EstablishmentBranch> _branches = [];
    public IReadOnlyList<EstablishmentBranch> Added => _branches;
    public int SaveChangesCallCount { get; private set; }

    public Task<EstablishmentBranch?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        EstablishmentBranch? found = _branches.FirstOrDefault(b => b.Id == id);
        return Task.FromResult(found);
    }

    public Task<bool> ExistsByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        bool exists = _branches.Any(b => b.Id == id);
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByNameWithinEstablishmentAsync(
        EstablishmentId establishmentId,
        string normalizedName,
        EstablishmentBranchId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _branches.Any(b =>
            b.EstablishmentId == establishmentId &&
            string.Equals(b.Name, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            (excludingId is null || b.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public void Add(EstablishmentBranch branch)
    {
        _branches.Add(branch);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(EstablishmentBranch branch)
    {
        _branches.Add(branch);
    }
}
