using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Roles;

namespace Lyria.Application.Features.Roles.GetById;

public sealed class GetRoleByIdQueryHandler(
    IRoleReadService readService)
    : IQueryHandler<GetRoleByIdQuery, Result<RoleResponse>>
{
    public async ValueTask<Result<RoleResponse>> Handle(
        GetRoleByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = new RoleId(query.Id);

        RoleResponse? response = await readService.GetByIdAsync(
            id, cancellationToken);

        if (response is null)
        {
            return Result.Failure<RoleResponse>(
                RoleErrors.NotFound(query.Id));
        }

        return Result.Success(response);
    }
}
