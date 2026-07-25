using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.PublicCatalog.GetBranchById;

public sealed record GetPublicBranchByIdQuery(Guid BranchId)
    : IQuery<Result<PublicBranchFullDetailResponse>>;
