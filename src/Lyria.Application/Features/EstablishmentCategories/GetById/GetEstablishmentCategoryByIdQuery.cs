using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.EstablishmentCategories.GetById;

public sealed record GetEstablishmentCategoryByIdQuery(Guid Id)
    : IQuery<Result<EstablishmentCategoryResponse>>;
