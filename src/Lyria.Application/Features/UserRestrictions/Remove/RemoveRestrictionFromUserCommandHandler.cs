using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;

namespace Lyria.Application.Features.UserRestrictions.Remove;

public sealed class RemoveRestrictionFromUserCommandHandler(
    IUserRestrictionRepository userRestrictionRepository)
    : ICommandHandler<RemoveRestrictionFromUserCommand>
{
    public async ValueTask<Result> Handle(
        RemoveRestrictionFromUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(command.UserId);
        var restrictionId = new RestrictionId(command.RestrictionId);

        UserRestriction? userRestriction = await userRestrictionRepository.GetByIdsAsync(
            userId, restrictionId, cancellationToken);

        if (userRestriction is null)
        {
            return Result.Failure(
                UserRestrictionErrors.NotFound(command.UserId, command.RestrictionId));
        }

        userRestrictionRepository.Remove(userRestriction);
        await userRestrictionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
