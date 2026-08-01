using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.UserRoles.Assign;

public sealed class AssignRoleToUserCommandHandler(
    IUserRoleRepository userRoleRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IEstablishmentReadService establishmentReadService,
    IEstablishmentBranchReadService branchReadService,
    TimeProvider timeProvider)
    : ICommandHandler<AssignRoleToUserCommand, UserRoleId>
{
    public async ValueTask<Result<UserRoleId>> Handle(
        AssignRoleToUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(command.UserId);

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserRoleId>(
                UserRoleErrors.UserNotFound(command.UserId));
        }

        var roleId = new RoleId(command.RoleId);

        Role? role = await roleRepository.GetByIdAsync(roleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<UserRoleId>(
                UserRoleErrors.RoleNotFound(command.RoleId));
        }

        if (!Enum.TryParse<ScopeType>(command.ScopeType, ignoreCase: true, out var scopeType))
        {
            return Result.Failure<UserRoleId>(UserRoleErrors.InvalidScope());
        }

        EstablishmentId? establishmentId = null;
        EstablishmentBranchId? branchId = null;

        if (command.EstablishmentId.HasValue)
        {
            establishmentId = new EstablishmentId(command.EstablishmentId.Value);

            var establishment = await establishmentReadService.GetByIdAsync(
                establishmentId.Value, cancellationToken);

            if (establishment is null)
            {
                return Result.Failure<UserRoleId>(
                    UserRoleErrors.EstablishmentNotFound(command.EstablishmentId.Value));
            }
        }

        if (command.BranchId.HasValue)
        {
            branchId = new EstablishmentBranchId(command.BranchId.Value);

            var branch = await branchReadService.GetByIdAsync(
                branchId.Value, cancellationToken);

            if (branch is null)
            {
                return Result.Failure<UserRoleId>(
                    UserRoleErrors.BranchNotFound(command.BranchId.Value));
            }

            if (scopeType == ScopeType.Branch &&
                command.EstablishmentId.HasValue &&
                branch.EstablishmentId != command.EstablishmentId.Value)
            {
                return Result.Failure<UserRoleId>(
                    UserRoleErrors.BranchDoesNotBelongToEstablishment());
            }
        }

        var id = UserRoleId.New();

        var userRole = UserRole.Assign(
            id,
            userId,
            roleId,
            scopeType,
            establishmentId,
            branchId,
            timeProvider.GetUtcNow().UtcDateTime);

        await userRoleRepository.AddAsync(userRole, cancellationToken);
        await userRoleRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(userRole.Id);
    }
}
