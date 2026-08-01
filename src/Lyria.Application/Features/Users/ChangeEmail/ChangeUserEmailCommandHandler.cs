using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.ChangeEmail;

public sealed class ChangeUserEmailCommandHandler(
    IUserRepository repository)
    : ICommandHandler<ChangeUserEmailCommand>
{
    public async ValueTask<Result> Handle(
        ChangeUserEmailCommand command,
        CancellationToken cancellationToken)
    {
        var id = new UserId(command.UserId);

        User? user = await repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        string normalizedEmail = User.NormalizeEmail(command.Email);

        bool emailExists = await repository.ExistsByEmailAsync(
            normalizedEmail, excludingId: id, cancellationToken);

        if (emailExists)
        {
            return Result.Failure(UserErrors.EmailAlreadyExists());
        }

        user.ChangeEmail(command.Email);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
