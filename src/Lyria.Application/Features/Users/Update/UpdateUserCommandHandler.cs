using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.Update;

public sealed class UpdateUserCommandHandler(
    IUserRepository repository)
    : ICommandHandler<UpdateUserCommand>
{
    public async ValueTask<Result> Handle(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var id = new UserId(command.UserId);

        User? user = await repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        user.UpdateProfile(
            command.Name,
            command.LastName,
            command.Phone,
            command.BirthDate,
            command.PhotoUrl);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
