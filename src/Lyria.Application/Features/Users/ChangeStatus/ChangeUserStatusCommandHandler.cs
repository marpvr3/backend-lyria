using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.ChangeStatus;

public sealed class ChangeUserStatusCommandHandler(
    IUserRepository repository)
    : ICommandHandler<ChangeUserStatusCommand>
{
    public async ValueTask<Result> Handle(
        ChangeUserStatusCommand command,
        CancellationToken cancellationToken)
    {
        var id = new UserId(command.UserId);

        User? user = await repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        if (!Enum.TryParse<UserStatus>(command.Status, ignoreCase: true, out var newStatus))
        {
            return Result.Failure(UserErrors.InvalidStatus());
        }

        try
        {
            user.ChangeStatus(newStatus);
        }
        catch (UserException ex)
        {
            return Result.Failure(
                Common.Errors.Error.Validation("Users.InvalidStatusTransition", ex.Message));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
