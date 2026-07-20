using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.Services.GetById;

public sealed record GetServiceByIdQuery(Guid Id)
    : IQuery<Result<ServiceResponse>>;
