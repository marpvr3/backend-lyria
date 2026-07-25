using Lyria.Application.Features.PublicCatalog;

namespace Lyria.Application.Abstractions.Persistence;

public interface IPublicCatalogReadService
{
    Task<PublicCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken);
}
