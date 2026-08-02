using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.UserRoles.GetById;

public sealed record GetUserRoleByIdQuery(Guid Id)
    : IQuery<Result<UserRoleResponse>>;
