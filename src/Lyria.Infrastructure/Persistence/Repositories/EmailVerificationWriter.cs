using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lyria.Infrastructure.Persistence.Repositories;

/// <summary>
/// Confirma las escrituras de la verificación de correo dentro de una transacción explícita.
/// </summary>
/// <remarks>
/// Sigue el mismo patrón que <see cref="MobileRegistrationWriter"/> y
/// <see cref="AuthenticationSessionWriter"/>: la operación se envuelve en la execution
/// strategy del proveedor, que es la forma correcta de combinar transacciones manuales
/// con reintentos si algún día se habilita <c>EnableRetryOnFailure</c>.
///
/// Ninguna de estas operaciones envía correo: el envío ocurre después, ya sin transacción
/// abierta.
/// </remarks>
internal sealed class EmailVerificationWriter(LyriaDbContext dbContext)
    : IEmailVerificationWriter
{
    public Task ResendAsync(
        IReadOnlyCollection<UserEmailVerification> revokedVerifications,
        UserEmailVerification createdVerification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(revokedVerifications);
        ArgumentNullException.ThrowIfNull(createdVerification);

        // Las verificaciones revocadas ya están bajo seguimiento con su FechaRevocacion
        // aplicada; se persisten junto con la inserción de la nueva. Si la inserción
        // falla, el ROLLBACK deja los códigos anteriores sin revocar.
        return ExecuteInTransactionAsync(
            async () =>
            {
                await dbContext.Set<UserEmailVerification>()
                    .AddAsync(createdVerification, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<bool> TryConfirmAsync(
        User user,
        UserEmailVerification verification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(verification);

        try
        {
            // El usuario y la verificación ya están bajo seguimiento con IsEmailVerified,
            // el estado Active y FechaUso aplicados. Ambas escrituras se confirman juntas
            // o ninguna queda confirmada.
            await ExecuteInTransactionAsync(
                () => dbContext.SaveChangesAsync(cancellationToken),
                cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // FechaUso es token de concurrencia: si otra solicitud canjeó el mismo código
            // primero, el UPDATE no encuentra la fila y la transacción ya quedó revertida.
            // El usuario no se activa dos veces.
            return false;
        }
    }

    public async Task RegisterFailedAttemptAsync(
        UserEmailVerification verification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(verification);

        try
        {
            await ExecuteInTransactionAsync(
                () => dbContext.SaveChangesAsync(cancellationToken),
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // El código se canjeó mientras se procesaba este intento. No hay nada que
            // contabilizar y el caso de uso ya devuelve el error genérico.
        }
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
