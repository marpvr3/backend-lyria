using Lyria.Application.Common;
using Lyria.Application.Features.Users;
using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserReadService
{
    Task<UserResponse?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken);

    Task<PagedResponse<UserListItemResponse>> ListAsync(
        UserListFilter filter,
        CancellationToken cancellationToken);
}
