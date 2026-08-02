using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.Features.UserRestrictions.Assign;

public sealed class AssignRestrictionToUserCommandHandler(
    IUserRestrictionRepository userRestrictionRepository,
    IUserRepository userRepository,
    IRestrictionRepository restrictionRepository,
    TimeProvider timeProvider)
    : ICommandHandler<AssignRestrictionToUserCommand>
{
    public async ValueTask<Result> Handle(
        AssignRestrictionToUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(command.UserId);
        var restrictionId = new RestrictionId(command.RestrictionId);

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserRestrictionErrors.UserNotFound(command.UserId));
        }

        Restriction? restriction = await restrictionRepository.GetByIdAsync(
            restrictionId, cancellationToken);

        if (restriction is null)
        {
            return Result.Failure(
                UserRestrictionErrors.RestrictionNotFound(command.RestrictionId));
        }

        bool exists = await userRestrictionRepository.ExistsAsync(
            userId, restrictionId, cancellationToken);

        if (exists)
        {
            return Result.Failure(
                UserRestrictionErrors.AlreadyExists(command.UserId, command.RestrictionId));
        }

        var userRestriction = UserRestriction.Create(
            userId,
            restrictionId,
            command.ImportanceLevel,
            timeProvider.GetUtcNow().UtcDateTime);

        userRestrictionRepository.Add(userRestriction);
        await userRestrictionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
