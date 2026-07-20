using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Establishments;
using Lyria.Domain.Establishments;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentReadService : IEstablishmentReadService
{
    private readonly List<EstablishmentResponse> _responses = [];

    public Task<EstablishmentResponse?> GetByIdAsync(
        EstablishmentId id,
        CancellationToken cancellationToken)
    {
        EstablishmentResponse? found = _responses.FirstOrDefault(r => r.Id == id.Value);
        return Task.FromResult(found);
    }

    public Task<PagedResponse<EstablishmentListItemResponse>> ListAsync(
        EstablishmentListFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<EstablishmentResponse> query = _responses;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                      r.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == filter.CategoryId.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        if (filter.IsVerified.HasValue)
        {
            query = query.Where(r => r.IsVerified == filter.IsVerified.Value);
        }

        var all = query.OrderBy(r => r.Name).ToList();
        int totalItems = all.Count;
        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new EstablishmentListItemResponse(
                r.Id, r.CategoryId, r.CategoryName, r.Name, r.Slug, r.IsVerified, r.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<EstablishmentListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }

    public void Seed(EstablishmentResponse response)
    {
        _responses.Add(response);
    }
}
