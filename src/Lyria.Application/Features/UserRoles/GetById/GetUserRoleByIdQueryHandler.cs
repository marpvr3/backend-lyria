using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Users.UserRoles;

namespace Lyria.Application.Features.UserRoles.GetById;

public sealed class GetUserRoleByIdQueryHandler(
    IUserRoleReadService readService)
    : IQueryHandler<GetUserRoleByIdQuery, Result<UserRoleResponse>>
{
    public async ValueTask<Result<UserRoleResponse>> Handle(
        GetUserRoleByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new UserRoleId(query.Id);

        UserRoleResponse? response = await readService.GetByIdAsync(
            id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<UserRoleResponse>(
                UserRoleErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
