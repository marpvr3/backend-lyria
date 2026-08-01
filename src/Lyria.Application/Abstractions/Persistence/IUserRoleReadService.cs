using Lyria.Application.Features.UserRoles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRoleReadService
{
    Task<IReadOnlyList<UserRoleResponse>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<UserRoleResponse?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken);
}
