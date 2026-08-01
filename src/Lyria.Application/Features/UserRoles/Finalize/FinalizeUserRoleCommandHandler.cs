using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.UserRoles.Finalize;

public sealed class FinalizeUserRoleCommandHandler(
    IUserRoleRepository userRoleRepository,
    TimeProvider timeProvider)
    : ICommandHandler<FinalizeUserRoleCommand>
{
    public async ValueTask<Result> Handle(
        FinalizeUserRoleCommand command,
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
            userRole.Finalize(timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (UserRoleException ex)
        {
            return Result.Failure(
                Common.Errors.Error.Validation("UserRoles.InvalidFinalization", ex.Message));
        }

        await userRoleRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
