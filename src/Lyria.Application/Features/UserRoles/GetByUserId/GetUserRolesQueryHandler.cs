using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users;

namespace Lyria.Application.Features.UserRoles.GetByUserId;

public sealed class GetUserRolesQueryHandler(
    IUserRoleReadService userRoleReadService,
    IUserReadService userReadService)
    : IQueryHandler<GetUserRolesQuery, Result<IReadOnlyList<UserRoleResponse>>>
{
    public async ValueTask<Result<IReadOnlyList<UserRoleResponse>>> Handle(
        GetUserRolesQuery query,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(query.UserId);

        var user = await userReadService.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<IReadOnlyList<UserRoleResponse>>(
                UserRoleErrors.UserNotFound(query.UserId));
        }

        IReadOnlyList<UserRoleResponse> roles = await userRoleReadService.GetByUserIdAsync(
            userId, cancellationToken);

        return Result.Success(roles);
    }
}
