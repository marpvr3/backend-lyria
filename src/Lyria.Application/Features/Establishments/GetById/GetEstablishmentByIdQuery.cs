using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Establishments.GetById;

public sealed record GetEstablishmentByIdQuery(Guid Id)
    : IQuery<Result<EstablishmentResponse>>;
