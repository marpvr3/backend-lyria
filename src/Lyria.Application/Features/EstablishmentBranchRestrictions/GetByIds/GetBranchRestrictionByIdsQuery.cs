using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.EstablishmentBranchRestrictions.GetByIds;

public sealed record GetBranchRestrictionByIdsQuery(
    Guid BranchId,
    Guid RestrictionId)
    : IQuery<Result<EstablishmentBranchRestrictionResponse>>;
