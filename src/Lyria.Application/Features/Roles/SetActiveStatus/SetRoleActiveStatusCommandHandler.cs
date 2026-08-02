using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.SetActiveStatus;

public sealed class SetRoleActiveStatusCommandHandler(
    IRoleRepository repository)
    : ICommandHandler<SetRoleActiveStatusCommand>
{
    public async ValueTask<Result> Handle(
        SetRoleActiveStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new RoleId(command.Id);

        Role? role = await repository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.Id));
        }

        try
        {
            if (command.IsActive)
            {
                role.Activate();
            }
            else
            {
                role.Deactivate();
            }
        }
        catch (RoleException ex)
        {
            return Result.Failure(
                Common.Errors.Error.Validation("Roles.InvalidStatusChange", ex.Message));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
