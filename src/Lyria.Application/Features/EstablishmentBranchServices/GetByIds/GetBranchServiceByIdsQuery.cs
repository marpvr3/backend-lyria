using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.EstablishmentBranchServices.GetByIds;

public sealed record GetBranchServiceByIdsQuery(
    Guid BranchId,
    Guid ServiceId)
    : IQuery<Result<EstablishmentBranchServiceResponse>>;
