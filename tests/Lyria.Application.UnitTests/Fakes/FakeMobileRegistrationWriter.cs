using Lyria.Application.Abstractions.Persistence;
using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.UnitTests.Fakes;

/// <summary>
/// Sustituto del coordinador transaccional. Registra lo que se le pide persistir
/// y solo lo "confirma" cuando la operación completa tiene éxito.
/// </summary>
internal sealed class FakeMobileRegistrationWriter : IMobileRegistrationWriter
{
    /// <summary>
    /// Excepción que debe lanzarse en lugar de confirmar. Simula un fallo de persistencia.
    /// </summary>
    public Exception? FailureToThrow { get; set; }

    public int RegisterCallCount { get; private set; }

    public User? CommittedUser { get; private set; }

    public UserRole? CommittedUserRole { get; private set; }

    public IReadOnlyCollection<UserRestriction> CommittedUserRestrictions { get; private set; } = [];

    public UserEmailVerification? CommittedEmailVerification { get; private set; }

    public bool Committed { get; private set; }

    public Task RegisterAsync(
        User user,
        UserRole userRole,
        IReadOnlyCollection<UserRestriction> userRestrictions,
        UserEmailVerification emailVerification,
        CancellationToken cancellationToken)
    {
        RegisterCallCount++;

        if (FailureToThrow is not null)
        {
            // Nada queda confirmado: es el equivalente en memoria de un ROLLBACK.
            throw FailureToThrow;
        }

        CommittedUser = user;
        CommittedUserRole = userRole;
        CommittedUserRestrictions = userRestrictions;
        CommittedEmailVerification = emailVerification;
        Committed = true;

        return Task.CompletedTask;
    }
}
