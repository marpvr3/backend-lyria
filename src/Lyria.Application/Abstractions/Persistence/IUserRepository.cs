using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        UserId? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
