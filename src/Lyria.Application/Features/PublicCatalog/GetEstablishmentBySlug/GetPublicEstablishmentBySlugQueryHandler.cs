using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;

public sealed class GetPublicEstablishmentBySlugQueryHandler(
    IPublicEstablishmentReadService readService)
    : IQueryHandler<GetPublicEstablishmentBySlugQuery, Result<PublicEstablishmentDetailResponse>>
{
    public async ValueTask<Result<PublicEstablishmentDetailResponse>> Handle(
        GetPublicEstablishmentBySlugQuery query,
        CancellationToken cancellationToken)
    {
        string normalizedSlug = Establishment.NormalizeSlug(query.Slug);

        var result = await readService.GetBySlugAsync(normalizedSlug, cancellationToken);

        return result is null
            ? Result.Failure<PublicEstablishmentDetailResponse>(
                PublicCatalogErrors.EstablishmentNotFound(normalizedSlug))
            : Result.Success(result);
    }
}
