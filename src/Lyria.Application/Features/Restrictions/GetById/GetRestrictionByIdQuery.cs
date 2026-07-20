using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Restrictions.GetById;

public sealed record GetRestrictionByIdQuery(Guid Id)
    : IQuery<Result<RestrictionResponse>>;
