using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Common;
using Lyria.Application.Common.Results;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishments;

public sealed class GetPublicEstablishmentsQueryHandler(
    IPublicEstablishmentReadService readService,
    TimeProvider timeProvider)
    : IQueryHandler<GetPublicEstablishmentsQuery, Result<PagedResponse<PublicEstablishmentListItemResponse>>>
{
    public async ValueTask<Result<PagedResponse<PublicEstablishmentListItemResponse>>> Handle(
        GetPublicEstablishmentsQuery query,
        CancellationToken cancellationToken)
    {
        DateTimeOffset evaluatedAtUtc = timeProvider.GetUtcNow();

        var filter = new PublicEstablishmentListFilter(
            query.Search?.Trim(),
            query.CategoryId,
            query.City?.Trim(),
            query.Province?.Trim(),
            query.Country?.Trim(),
            query.ServiceId,
            query.RestrictionId,
            query.ComplianceLevel,
            query.IsCertified,
            query.OpenNow,
            query.Page,
            query.PageSize,
            query.SortBy.Trim().ToLowerInvariant(),
            query.SortDirection.Trim().ToLowerInvariant(),
            evaluatedAtUtc);

        var result = await readService.ListAsync(filter, cancellationToken);

        return Result.Success(result);
    }
}
