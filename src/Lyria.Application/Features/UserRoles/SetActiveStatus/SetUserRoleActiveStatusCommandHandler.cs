using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.UserRoles.SetActiveStatus;

public sealed class SetUserRoleActiveStatusCommandHandler(
    IUserRoleRepository userRoleRepository)
    : ICommandHandler<SetUserRoleActiveStatusCommand>
{
    public async ValueTask<Result> Handle(
        SetUserRoleActiveStatusCommand command,
        CancellationToken cancellationToken)
    {
        var userRoleId = new UserRoleId(command.UserRoleId);

        UserRole? userRole = await userRoleRepository.GetByIdAsync(
            userRoleId, cancellationToken);

        if (userRole is null)
        {
            return Result.Failure(UserRoleErrors.NotFound(command.UserRoleId));
        }

        var userId = new UserId(command.UserId);

        if (userRole.UserId != userId)
        {
            return Result.Failure(UserRoleErrors.NotFound(command.UserRoleId));
        }

        try
        {
            if (command.IsActive)
            {
                userRole.Activate();
            }
            else
            {
                userRole.Deactivate();
            }
        }
        catch (UserRoleException ex)
        {
            return Result.Failure(
                Common.Errors.Error.Validation("UserRoles.InvalidStatusChange", ex.Message));
        }

        await userRoleRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
