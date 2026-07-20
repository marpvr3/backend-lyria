using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Restrictions;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeRestrictionReadService : IRestrictionReadService
{
    private readonly List<RestrictionResponse> _restrictions = [];

    public void Seed(RestrictionResponse response) => _restrictions.Add(response);

    public Task<RestrictionResponse?> GetByIdAsync(
        RestrictionId id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_restrictions.FirstOrDefault(r => r.Id == id.Value));
    }

    public Task<PagedResponse<RestrictionListItemResponse>> ListAsync(
        RestrictionListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _restrictions.AsEnumerable();

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
            .Select(r => new RestrictionListItemResponse(r.Id, r.Name, r.Description, r.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<RestrictionListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }
}
