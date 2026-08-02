using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.UserRoles.GetByUserId;

public sealed record GetUserRolesQuery(Guid UserId)
    : IQuery<Result<IReadOnlyList<UserRoleResponse>>>;
