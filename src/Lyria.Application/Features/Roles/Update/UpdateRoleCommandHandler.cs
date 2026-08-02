using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.Update;

public sealed class UpdateRoleCommandHandler(
    IRoleRepository repository)
    : ICommandHandler<UpdateRoleCommand>
{
    public async ValueTask<Result> Handle(
        UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var id = new RoleId(command.Id);

        Role? role = await repository.GetByIdAsync(id, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(command.Id));
        }

        role.Update(command.Name, command.Description);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
