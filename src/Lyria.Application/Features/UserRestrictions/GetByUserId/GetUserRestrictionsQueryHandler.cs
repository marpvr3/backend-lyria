using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.Users;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.UserRestrictions.GetByUserId;

public sealed class GetUserRestrictionsQueryHandler(
    IUserRestrictionReadService userRestrictionReadService,
    IUserReadService userReadService)
    : IQueryHandler<GetUserRestrictionsQuery, Result<IReadOnlyList<UserRestrictionResponse>>>
{
    public async ValueTask<Result<IReadOnlyList<UserRestrictionResponse>>> Handle(
        GetUserRestrictionsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(query.UserId);

        UserResponse? user = await userReadService.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<IReadOnlyList<UserRestrictionResponse>>(
                UserRestrictionErrors.UserNotFound(query.UserId));
        }

        IReadOnlyList<UserRestrictionResponse> restrictions =
            await userRestrictionReadService.GetByUserIdAsync(userId, cancellationToken);

        return Result.Success(restrictions);
    }
}
