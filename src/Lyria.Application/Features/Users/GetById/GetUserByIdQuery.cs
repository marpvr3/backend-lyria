using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Users.GetById;

public sealed record GetUserByIdQuery(Guid Id)
    : IQuery<Result<UserResponse>>;
