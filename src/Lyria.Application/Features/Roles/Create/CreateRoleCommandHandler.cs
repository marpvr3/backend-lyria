using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.Create;

public sealed class CreateRoleCommandHandler(
    IRoleRepository repository)
    : ICommandHandler<CreateRoleCommand, RoleId>
{
    public async ValueTask<Result<RoleId>> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedCode = Role.NormalizeCode(command.Code);

        bool codeExists = await repository.ExistsByCodeAsync(
            normalizedCode, excludingId: null, cancellationToken);

        if (codeExists)
        {
            return Result.Failure<RoleId>(RoleErrors.CodeAlreadyExists());
        }

        var id = RoleId.New();

        var role = Role.Create(
            id,
            command.Code,
            command.Name,
            command.Description);

        await repository.AddAsync(role, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(role.Id);
    }
}
