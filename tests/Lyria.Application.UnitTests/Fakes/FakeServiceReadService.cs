using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Services;
using Lyria.Domain.Services;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeServiceReadService : IServiceReadService
{
    private readonly List<ServiceResponse> _services = [];

    public void Seed(ServiceResponse response) => _services.Add(response);

    public Task<ServiceResponse?> GetByIdAsync(
        ServiceId id,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_services.FirstOrDefault(s => s.Id == id.Value));
    }

    public Task<PagedResponse<ServiceListItemResponse>> ListAsync(
        ServiceListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _services.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == filter.IsActive.Value);
        }

        var all = query.OrderBy(s => s.Name).ToList();
        int totalItems = all.Count;

        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new ServiceListItemResponse(s.Id, s.Name, s.Description, s.IconUrl, s.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<ServiceListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }
}
