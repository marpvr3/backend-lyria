using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.EstablishmentBranchServices;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeEstablishmentBranchServiceReadService : IEstablishmentBranchServiceReadService
{
    private readonly List<EstablishmentBranchServiceResponse> _items = [];

    public void Seed(EstablishmentBranchServiceResponse response) => _items.Add(response);

    public Task<EstablishmentBranchServiceResponse?> GetByIdsAsync(
        EstablishmentBranchId branchId,
        ServiceId serviceId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(
            x => x.BranchId == branchId.Value && x.ServiceId == serviceId.Value));
    }

    public Task<PagedResponse<EstablishmentBranchServiceListItemResponse>> ListByBranchAsync(
        EstablishmentBranchServiceListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _items
            .Where(x => x.BranchId == filter.BranchId.Value)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(x => x.ServiceName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(x => x.IsAvailable == filter.IsAvailable.Value);
        }

        var all = query.OrderBy(x => x.ServiceName).ToList();
        int totalItems = all.Count;

        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EstablishmentBranchServiceListItemResponse(
                x.BranchId, x.ServiceId, x.ServiceName, x.ServiceIconUrl,
                x.IsAvailable, x.Observation, x.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<EstablishmentBranchServiceListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }
}
