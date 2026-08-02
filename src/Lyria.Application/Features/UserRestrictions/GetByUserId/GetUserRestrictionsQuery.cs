using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.UserRestrictions.GetByUserId;

public sealed record GetUserRestrictionsQuery(Guid UserId)
    : IQuery<Result<IReadOnlyList<UserRestrictionResponse>>>;
