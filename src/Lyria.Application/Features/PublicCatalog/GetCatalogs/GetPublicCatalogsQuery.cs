using Lyria.Application.Abstractions.Messaging;

namespace Lyria.Application.Features.PublicCatalog.GetCatalogs;

public sealed record GetPublicCatalogsQuery()
    : IQuery<PublicCatalogsResponse>;
