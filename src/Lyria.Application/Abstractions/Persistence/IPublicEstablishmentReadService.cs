using Lyria.Application.Common;
using Lyria.Application.Features.PublicCatalog;

namespace Lyria.Application.Abstractions.Persistence;

public interface IPublicEstablishmentReadService
{
    Task<PagedResponse<PublicEstablishmentListItemResponse>> ListAsync(
        PublicEstablishmentListFilter filter,
        CancellationToken cancellationToken);

    Task<PublicEstablishmentDetailResponse?> GetBySlugAsync(
        string normalizedSlug,
        CancellationToken cancellationToken);
}
