using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Domain.Establishments.Categories;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentCategoryReadService : IEstablishmentCategoryReadService
{
    private readonly List<EstablishmentCategoryResponse> _responses = [];

    public Task<EstablishmentCategoryResponse?> GetActiveByIdAsync(
        EstablishmentCategoryId id,
        CancellationToken cancellationToken)
    {
        EstablishmentCategoryResponse? found = _responses
            .FirstOrDefault(r => r.Id == id.Value && r.IsActive);
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<EstablishmentCategoryResponse>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EstablishmentCategoryResponse> active = _responses
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToList();
        return Task.FromResult(active);
    }

    public void Seed(EstablishmentCategoryResponse response)
    {
        _responses.Add(response);
    }
}
