using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Features.Users;
using Lyria.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.ReadServices;

internal sealed class UserReadService(LyriaDbContext dbContext)
    : IUserReadService
{
    public async Task<UserResponse?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(
                u.Id.Value,
                u.Name,
                u.LastName,
                u.Email,
                u.Phone,
                u.BirthDate,
                u.PhotoUrl,
                u.Status.ToString(),
                u.IsEmailVerified,
                u.LastLoginAtUtc,
                u.CreatedAtUtc,
                u.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<UserListItemResponse>> ListAsync(
        UserListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<User>()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();
            query = query.Where(u =>
                u.Name.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            string email = filter.Email.Trim();
            query = query.Where(u => u.Email == email);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<UserStatus>(filter.Status, ignoreCase: true, out var status))
            {
                query = query.Where(u => u.Status == status);
            }
        }

        if (filter.EmailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == filter.EmailVerified.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        bool descending = string.Equals(
            filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<User> orderedQuery = filter.SortBy?.ToLowerInvariant() switch
        {
            "lastname" => descending
                ? query.OrderByDescending(u => u.LastName)
                : query.OrderBy(u => u.LastName),
            "email" => descending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            "createdatutc" => descending
                ? query.OrderByDescending(u => u.CreatedAtUtc)
                : query.OrderBy(u => u.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(u => u.Name)
                : query.OrderBy(u => u.Name)
        };

        var items = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserListItemResponse(
                u.Id.Value,
                u.Name,
                u.LastName,
                u.Email,
                u.Status.ToString(),
                u.IsEmailVerified))
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserListItemResponse>(
            items, filter.Page, filter.PageSize, totalItems);
    }
}
