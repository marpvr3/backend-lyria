using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.BranchImages.GetByBranch;

public sealed record GetBranchImagesQuery(Guid BranchId)
    : IQuery<Result<BranchImagesResponse>>;
