using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lyria.Infrastructure.Persistence.Repositories;

/// <summary>
/// Confirma las escrituras de la autenticación móvil dentro de una transacción explícita.
/// </summary>
/// <remarks>
/// Sigue el mismo patrón que <see cref="MobileRegistrationWriter"/>: la operación se
/// envuelve en la execution strategy del proveedor, que es la forma correcta de
/// combinar transacciones manuales con reintentos si algún día se habilita
/// <c>EnableRetryOnFailure</c>. Hoy la estrategia predeterminada de SQL Server no
/// reintenta.
///
/// La transacción vive completamente en Infrastructure: ningún repositorio abre la
/// suya y Application nunca controla el commit.
/// </remarks>
internal sealed class AuthenticationSessionWriter(LyriaDbContext dbContext)
    : IAuthenticationSessionWriter
{
    public Task CompleteLoginAsync(
        User user,
        UserRefreshToken session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(session);

        // El usuario ya está bajo seguimiento con la última conexión —y, si procedía, el
        // hash regenerado— aplicados. Ambas escrituras se confirman con la sesión o
        // ninguna queda confirmada.
        return ExecuteInTransactionAsync(
            async () =>
            {
                await dbContext.Set<UserRefreshToken>()
                    .AddAsync(session, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
            },
            cancellationToken);
    }

    public Task RotateAsync(
        UserRefreshToken revokedSession,
        UserRefreshToken createdSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(revokedSession);
        ArgumentNullException.ThrowIfNull(createdSession);

        return ExecuteInTransactionAsync(
            async () =>
            {
                // La sesión revocada ya está bajo seguimiento; se persiste su
                // FechaRevocacion junto con la inserción de la sesión nueva. Si la
                // inserción falla, el ROLLBACK deja el token anterior sin revocar.
                await dbContext.Set<UserRefreshToken>()
                    .AddAsync(createdSession, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
            },
            cancellationToken);
    }

    public Task RevokeAsync(
        UserRefreshToken revokedSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(revokedSession);

        // Revocación lógica: la fila se conserva y solo se establece FechaRevocacion.
        return ExecuteInTransactionAsync(
            () => dbContext.SaveChangesAsync(cancellationToken),
            cancellationToken);
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation();

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
