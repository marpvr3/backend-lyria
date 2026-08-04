using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lyria.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persiste el registro móvil (usuario, rol y restricciones alimenticias)
/// dentro de una única transacción explícita.
/// </summary>
/// <remarks>
/// La secuencia es BEGIN TRANSACTION → INSERT Usuarios → INSERT UsuarioRoles →
/// INSERT UsuarioRestricciones → COMMIT. Ante cualquier fallo se ejecuta ROLLBACK
/// y la excepción se propaga sin enmascararse.
///
/// La operación se envuelve en la execution strategy del proveedor: hoy la estrategia
/// predeterminada de SQL Server no reintenta (no hay EnableRetryOnFailure configurado),
/// pero usarla es el patrón correcto para combinar reintentos con transacciones
/// manuales si algún día se habilitan.
/// </remarks>
internal sealed class MobileRegistrationWriter(LyriaDbContext dbContext)
    : IMobileRegistrationWriter
{
    public async Task RegisterAsync(
        User user,
        UserRole userRole,
        IReadOnlyCollection<UserRestriction> userRestrictions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userRestrictions);

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await dbContext.Set<User>().AddAsync(user, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await dbContext.Set<UserRole>().AddAsync(userRole, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                if (userRestrictions.Count > 0)
                {
                    await dbContext.Set<UserRestriction>()
                        .AddRangeAsync(userRestrictions, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

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
