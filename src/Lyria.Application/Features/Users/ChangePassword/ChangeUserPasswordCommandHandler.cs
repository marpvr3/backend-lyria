using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.ChangePassword;

public sealed class ChangeUserPasswordCommandHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangeUserPasswordCommand>
{
    public async ValueTask<Result> Handle(
        ChangeUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var id = new UserId(command.UserId);

        User? user = await repository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        string passwordHash = passwordHasher.Hash(command.Password);

        user.ChangePasswordHash(passwordHash);

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
