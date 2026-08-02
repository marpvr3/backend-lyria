using Lyria.Domain.Roles;

namespace Lyria.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(
        RoleId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        RoleId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Role role,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
