using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.UserRestrictions.GetById;

public sealed record GetUserRestrictionByIdQuery(
    Guid UserId,
    Guid RestrictionId)
    : IQuery<Result<UserRestrictionResponse>>;
