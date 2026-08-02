using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Users;
using Lyria.Domain.Users;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakeUserReadService : IUserReadService
{
    private readonly List<UserResponse> _responses = [];

    public Task<UserResponse?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken)
    {
        UserResponse? found = _responses.FirstOrDefault(r => r.Id == id.Value);
        return Task.FromResult(found);
    }

    public Task<PagedResponse<UserListItemResponse>> ListAsync(
        UserListFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<UserResponse> query = _responses;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(r =>
                r.Email.Contains(filter.Email, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(r =>
                string.Equals(r.Status, filter.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.EmailVerified.HasValue)
        {
            query = query.Where(r => r.IsEmailVerified == filter.EmailVerified.Value);
        }

        var all = query.OrderBy(r => r.Name).ToList();
        int totalItems = all.Count;
        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new UserListItemResponse(
                r.Id, r.Name, r.LastName, r.Email, r.Status, r.IsEmailVerified))
            .ToList();

        return Task.FromResult(new PagedResponse<UserListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems));
    }

    public void Seed(UserResponse response)
    {
        _responses.Add(response);
    }
}
