using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Security;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.Users.Create;

public sealed class CreateUserCommandHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, UserId>
{
    public async ValueTask<Result<UserId>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = User.NormalizeEmail(command.Email);

        bool emailExists = await repository.ExistsByEmailAsync(
            normalizedEmail, excludingId: null, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<UserId>(UserErrors.EmailAlreadyExists());
        }

        string passwordHash = passwordHasher.Hash(command.Password);

        var id = UserId.New();

        var user = User.Create(
            id,
            command.Name,
            command.LastName,
            command.Email,
            passwordHash,
            command.Phone,
            command.BirthDate,
            command.PhotoUrl);

        await repository.AddAsync(user, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
