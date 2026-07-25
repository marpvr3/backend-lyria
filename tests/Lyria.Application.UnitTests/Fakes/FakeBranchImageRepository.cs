using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeBranchImageRepository : IBranchImageRepository
{
    private readonly List<BranchImage> _images = [];
    public IReadOnlyList<BranchImage> Added => _images;
    public int SaveChangesCallCount { get; private set; }

    public void Seed(BranchImage image) => _images.Add(image);

    public Task<BranchImage?> GetByIdAsync(
        BranchImageId id,
        CancellationToken cancellationToken)
    {
        BranchImage? found = _images.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(found);
    }

    public Task<List<BranchImage>> GetActiveByBranchIdAsync(
        EstablishmentBranchId branchId,
        CancellationToken cancellationToken)
    {
        var result = _images
            .Where(i => i.BranchId == branchId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(BranchImage image)
    {
        _images.Add(image);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
