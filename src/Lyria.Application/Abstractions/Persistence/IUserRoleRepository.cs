using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRoleRepository
{
    Task<UserRole?> GetByIdAsync(
        UserRoleId id,
        CancellationToken cancellationToken);

    Task AddAsync(
        UserRole userRole,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
