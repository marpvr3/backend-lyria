using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranches;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchReadService : IEstablishmentBranchReadService
{
    private readonly List<EstablishmentBranchResponse> _responses = [];

    public Task<EstablishmentBranchResponse?> GetByIdAsync(
        EstablishmentBranchId id,
        CancellationToken cancellationToken)
    {
        EstablishmentBranchResponse? found = _responses.FirstOrDefault(r => r.Id == id.Value);
        return Task.FromResult(found);
    }

    public Task<PagedResponse<EstablishmentBranchListItemResponse>> ListByEstablishmentAsync(
        EstablishmentBranchListFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<EstablishmentBranchResponse> query = _responses
            .Where(r => r.EstablishmentId == filter.EstablishmentId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        var all = query.OrderBy(r => r.Name).ToList();
        int totalItems = all.Count;
        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new EstablishmentBranchListItemResponse(
                r.Id, r.EstablishmentId, r.Name, r.FullAddress,
                r.City, r.Province, r.Latitude, r.Longitude,
                r.RatingAverage, r.TotalReviews, r.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<EstablishmentBranchListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }

    public void Seed(EstablishmentBranchResponse response)
    {
        _responses.Add(response);
    }
}
