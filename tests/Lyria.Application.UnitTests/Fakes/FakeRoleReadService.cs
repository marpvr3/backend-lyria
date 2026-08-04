using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Roles;
using Lyria.Domain.Roles;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeRoleReadService : IRoleReadService
{
    private readonly List<RoleResponse> _responses = [];

    public Task<RoleResponse?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken)
    {
        RoleResponse? found = _responses.FirstOrDefault(r => r.Id == id.Value);
        return Task.FromResult(found);
    }

    public Task<PagedResponse<RoleListItemResponse>> ListAsync(
        RoleListFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<RoleResponse> query = _responses;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
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
            .Select(r => new RoleListItemResponse(r.Id, r.Name, r.IsActive))
            .ToList();

        return Task.FromResult(new PagedResponse<RoleListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }

    public void Seed(RoleResponse response)
    {
        _responses.Add(response);
    }
}
