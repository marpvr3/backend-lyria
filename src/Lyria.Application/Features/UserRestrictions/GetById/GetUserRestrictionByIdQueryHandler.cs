using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.UserRestrictions.GetById;

public sealed class GetUserRestrictionByIdQueryHandler(
    IUserRestrictionReadService userRestrictionReadService)
    : IQueryHandler<GetUserRestrictionByIdQuery, Result<UserRestrictionResponse>>
{
    public async ValueTask<Result<UserRestrictionResponse>> Handle(
        GetUserRestrictionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(query.UserId);
        var restrictionId = new RestrictionId(query.RestrictionId);

        UserRestrictionResponse? response = await userRestrictionReadService.GetByIdsAsync(
            userId, restrictionId, cancellationToken);

        if (response is null)
        {
            return Result.Failure<UserRestrictionResponse>(
                UserRestrictionErrors.NotFound(query.UserId, query.RestrictionId));
        }

        return Result.Success(response);
    }
}
