using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentCategoryRepository : IEstablishmentCategoryRepository
{
    private readonly List<EstablishmentCategory> _categories = [];
    public IReadOnlyList<EstablishmentCategory> Added => _categories;
    public int SaveChangesCallCount { get; private set; }

    public Task<EstablishmentCategory?> GetByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken)
    {
        EstablishmentCategory? found = _categories.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(found);
    }

    public Task<bool> ExistsByNameAsync(
        string normalizedName,
        EstablishmentCategoryId? excludingId,
        CancellationToken cancellationToken)
    {
        bool exists = _categories.Any(c =>
            c.Name == normalizedName &&
            (excludingId is null || c.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        EstablishmentCategory category,
        CancellationToken cancellationToken)
    {
        _categories.Add(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    public void Seed(EstablishmentCategory category)
    {
        _categories.Add(category);
    }
}
