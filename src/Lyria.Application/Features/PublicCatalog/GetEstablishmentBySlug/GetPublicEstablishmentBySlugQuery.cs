using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;

public sealed record GetPublicEstablishmentBySlugQuery(string Slug)
    : IQuery<Result<PublicEstablishmentDetailResponse>>;
