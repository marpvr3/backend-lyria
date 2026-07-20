using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.EstablishmentBranches.GetById;

public sealed record GetEstablishmentBranchByIdQuery(Guid Id)
    : IQuery<Result<EstablishmentBranchResponse>>;
