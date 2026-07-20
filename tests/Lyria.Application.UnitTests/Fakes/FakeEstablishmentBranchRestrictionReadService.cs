using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchRestrictions;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchRestrictionReadService : IEstablishmentBranchRestrictionReadService
{
    private readonly List<EstablishmentBranchRestrictionResponse> _items = [];

    public void Seed(EstablishmentBranchRestrictionResponse response) => _items.Add(response);

    public Task<EstablishmentBranchRestrictionResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        RestrictionId restrictionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(
            x => x.BranchId == branchId.Value && x.RestrictionId == restrictionId.Value));
    }

    public Task<PagedResponse<EstablishmentBranchRestrictionListItemResponse>> ListByBranchAsync(
        EstablishmentBranchRestrictionListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _items
            .Where(x => x.BranchId == filter.BranchId.Value)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(x => x.RestrictionName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        if (filter.ComplianceLevel.HasValue)
        {
            query = query.Where(x => x.ComplianceLevel == filter.ComplianceLevel.Value);
        }

        if (filter.IsCertified.HasValue)
        {
            query = query.Where(x => x.IsCertified == filter.IsCertified.Value);
        }

        var all = query.OrderBy(x => x.RestrictionName).ToList();
        int totalItems = all.Count;

        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EstablishmentBranchRestrictionListItemResponse(
                x.BranchId, x.RestrictionId, x.RestrictionName,
                x.ComplianceLevel, x.IsCertified, x.Observation, x.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<EstablishmentBranchRestrictionListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }
}
