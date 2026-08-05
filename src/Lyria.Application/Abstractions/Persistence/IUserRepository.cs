using Lyria.Domain.Users;

namespace Lyria.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        UserId id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Recupera el usuario cuyo correo normalizado coincide con el indicado.
    /// </summary>
    /// <param name="normalizedEmail">
    /// Correo ya normalizado con <see cref="User.NormalizeEmail"/>.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<User?> GetByEmailAsync(
        string normalizedEmail,
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
