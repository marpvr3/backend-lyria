using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Features.PublicCatalog;

namespace Lyria.Application.UnitTests.Fakes;

internal sealed class FakePublicCatalogReadService : IPublicCatalogReadService
{
    private PublicCatalogsResponse _response = new([], [], [],
        new PublicCatalogLocationsResponse([], [], []));

    public void Seed(PublicCatalogsResponse response)
    {
        _response = response;
    }

    public Task<PublicCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
