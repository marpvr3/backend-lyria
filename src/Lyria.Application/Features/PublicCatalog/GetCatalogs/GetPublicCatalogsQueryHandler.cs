using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;

namespace Lyria.Application.Features.PublicCatalog.GetCatalogs;

public sealed class GetPublicCatalogsQueryHandler(
    IPublicCatalogReadService readService)
    : IQueryHandler<GetPublicCatalogsQuery, PublicCatalogsResponse>
{
    public async ValueTask<PublicCatalogsResponse> Handle(
        GetPublicCatalogsQuery query,
        CancellationToken cancellationToken)
    {
        return await readService.GetCatalogsAsync(cancellationToken);
    }
}
