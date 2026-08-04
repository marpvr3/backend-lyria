using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Abstractions.Persistence;

/// <summary>
/// Coordinador de persistencia específico del registro móvil.
/// Persiste el usuario, su asignación de rol y sus restricciones alimenticias
/// dentro de una única transacción de base de datos.
/// </summary>
/// <remarks>
/// No es un Unit of Work genérico: existe únicamente para este caso de uso y no
/// expone control transaccional al resto de la aplicación. La transacción vive
/// completamente en Infrastructure.
/// </remarks>
public interface IMobileRegistrationWriter
{
    /// <summary>
    /// Persiste las tres escrituras del registro móvil de forma atómica.
    /// Si cualquiera falla, ninguna queda confirmada.
    /// </summary>
    /// <param name="user">Usuario a crear.</param>
    /// <param name="userRole">Asignación del rol base al usuario.</param>
    /// <param name="userRestrictions">
    /// Restricciones alimenticias del usuario. Puede estar vacía.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task RegisterAsync(
        User user,
        UserRole userRole,
        IReadOnlyCollection<UserRestriction> userRestrictions,
        CancellationToken cancellationToken);
}
