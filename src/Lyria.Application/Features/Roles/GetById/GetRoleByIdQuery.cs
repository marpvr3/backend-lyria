using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Roles.GetById;

public sealed record GetRoleByIdQuery(Guid Id)
    : IQuery<Result<RoleResponse>>;
