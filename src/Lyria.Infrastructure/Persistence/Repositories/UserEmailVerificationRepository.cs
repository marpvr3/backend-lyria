using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.EntityFrameworkCore;

namespace Lyria.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consulta las verificaciones de correo de un usuario.
/// </summary>
/// <remarks>
/// Las entidades se devuelven con seguimiento: la confirmación y el reenvío las modifican
/// y confirman el cambio a través de <see cref="IEmailVerificationWriter"/>.
/// </remarks>
internal sealed class UserEmailVerificationRepository(LyriaDbContext dbContext)
    : IUserEmailVerificationRepository
{
    public Task<UserEmailVerification?> GetLatestPendingAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        PendingQuery(userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<UserEmailVerification>> GetPendingAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        await PendingQuery(userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<DateTime?> GetLastCreatedAtUtcAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        // Se consideran todas las verificaciones, también las usadas y las revocadas: el
        // intervalo mínimo protege del envío repetido de correos, no del canje.
        List<DateTime> latest = await dbContext.Set<UserEmailVerification>()
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => v.CreatedAtUtc)
            .Take(1)
            .ToListAsync(cancellationToken);

        return latest.Count == 0 ? null : latest[0];
    }

    private IQueryable<UserEmailVerification> PendingQuery(UserId userId) =>
        dbContext.Set<UserEmailVerification>()
            .Where(v => v.UserId == userId &&
                        v.UsedAtUtc == null &&
                        v.RevokedAtUtc == null);
}
