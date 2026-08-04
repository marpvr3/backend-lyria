using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.MobileRegistrations.Register;

/// <summary>
/// Coordina el registro móvil: valida, construye las entidades de dominio y delega
/// la persistencia atómica en <see cref="IMobileRegistrationWriter"/>.
/// </summary>
/// <remarks>
/// El rol base se localiza exclusivamente por su identificador configurado.
/// Nunca se busca por Name, Description ni por primera coincidencia.
/// </remarks>
public sealed class RegisterMobileUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRestrictionRepository restrictionRepository,
    IMobileRegistrationWriter registrationWriter,
    IMobileRegistrationDefaults defaults,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterMobileUserCommand, MobileRegistrationResponse>
{
    public async ValueTask<Result<MobileRegistrationResponse>> Handle(
        RegisterMobileUserCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Configuración del rol base.
        if (!Guid.TryParse(defaults.DefaultRoleId, out Guid configuredRoleId) ||
            configuredRoleId == Guid.Empty)
        {
            return Result.Failure<MobileRegistrationResponse>(
                MobileRegistrationErrors.RoleConfigurationError());
        }

        // 2. Nivel de importancia predeterminado.
        string importanceLevel = UserRestriction.NormalizeImportanceLevel(
            defaults.DefaultRestrictionImportanceLevel);

        if (!UserRestrictionImportanceLevels.IsValid(importanceLevel))
        {
            return Result.Failure<MobileRegistrationResponse>(
                MobileRegistrationErrors.InvalidDefaultImportanceLevel());
        }

        // 3. Correo no registrado.
        string normalizedEmail = User.NormalizeEmail(command.Email);

        bool emailExists = await userRepository.ExistsByEmailAsync(
            normalizedEmail, excludingId: null, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<MobileRegistrationResponse>(
                MobileRegistrationErrors.EmailAlreadyExists());
        }

        // 4. Rol existente y activo. Un rol inexistente o inactivo es, igual que una
        //    configuración mal formada, un problema interno ajeno al cliente móvil.
        var roleId = new RoleId(configuredRoleId);

        Role? role = await roleRepository.GetByIdAsync(roleId, cancellationToken);

        if (role is null || !role.IsActive)
        {
            return Result.Failure<MobileRegistrationResponse>(
                MobileRegistrationErrors.RoleConfigurationError());
        }

        // 5. Todas las restricciones deben existir y estar disponibles en el catálogo.
        IReadOnlyList<Guid> restrictionIds = command.RestrictionIds ?? [];

        foreach (Guid restrictionId in restrictionIds)
        {
            Restriction? restriction = await restrictionRepository.GetByIdAsync(
                new RestrictionId(restrictionId), cancellationToken);

            if (restriction is null)
            {
                return Result.Failure<MobileRegistrationResponse>(
                    MobileRegistrationErrors.RestrictionNotFound(restrictionId));
            }

            if (!restriction.IsActive)
            {
                return Result.Failure<MobileRegistrationResponse>(
                    MobileRegistrationErrors.RestrictionNotAvailable(restrictionId));
            }
        }

        // 6. Hash seguro de la contraseña.
        string passwordHash = passwordHasher.Hash(command.Password);

        // 7. Entidades de dominio.
        var userId = UserId.New();

        var user = User.Create(
            userId,
            command.Name,
            command.LastName,
            command.Email,
            passwordHash,
            command.Phone,
            command.BirthDate,
            command.PhotoUrl);

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var userRole = UserRole.Assign(
            UserRoleId.New(),
            userId,
            roleId,
            ScopeType.Global,
            establishmentId: null,
            branchId: null,
            utcNow);

        List<UserRestriction> userRestrictions = [.. restrictionIds.Select(
            restrictionId => UserRestriction.Create(
                userId,
                new RestrictionId(restrictionId),
                importanceLevel,
                utcNow))];

        // 8. Persistencia atómica de las tres escrituras.
        await registrationWriter.RegisterAsync(
            user, userRole, userRestrictions, cancellationToken);

        return Result.Success(new MobileRegistrationResponse(
            userId.Value,
            user.Name,
            user.LastName,
            user.Email,
            user.Status.ToString(),
            restrictionIds));
    }
}
