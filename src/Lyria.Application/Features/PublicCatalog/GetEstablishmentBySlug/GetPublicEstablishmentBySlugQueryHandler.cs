using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.PublicCatalog.GetEstablishmentBySlug;

public sealed class GetPublicEstablishmentBySlugQueryHandler(
    IPublicEstablishmentReadService readService,
    IBranchAvailabilityReadService availabilityReadService,
    ITimeZoneService timeZoneService,
    TimeProvider timeProvider)
    : IQueryHandler<GetPublicEstablishmentBySlugQuery, Result<PublicEstablishmentDetailResponse>>
{
    public async ValueTask<Result<PublicEstablishmentDetailResponse>> Handle(
        GetPublicEstablishmentBySlugQuery query,
        CancellationToken cancellationToken)
    {
        string normalizedSlug = Establishment.NormalizeSlug(query.Slug);

        var result = await readService.GetBySlugAsync(normalizedSlug, cancellationToken);

        if (result is null)
        {
            return Result.Failure<PublicEstablishmentDetailResponse>(
                PublicCatalogErrors.EstablishmentNotFound(normalizedSlug));
        }

        DateTimeOffset evaluatedAtUtc = timeProvider.GetUtcNow();

        var branchIds = result.Branches
            .Select(b => new EstablishmentBranchId(b.Id))
            .ToList();

        var contexts = await availabilityReadService.GetAvailabilityContextsAsync(
            branchIds, evaluatedAtUtc, timeZoneService, cancellationToken);

        var branchesWithAvailability = result.Branches
            .Select(b =>
            {
                var branchId = new EstablishmentBranchId(b.Id);
                if (!contexts.TryGetValue(branchId, out var context))
                {
                    return b with { Availability = PublicBranchAvailabilityResponse.CreateNoSchedule(evaluatedAtUtc) };
                }

                DateTime localDateTime = timeZoneService.ConvertUtcToLocal(evaluatedAtUtc, context.TimeZoneId);
                var availability = BranchAvailabilityCalculator.Calculate(
                    localDateTime,
                    context.PreviousDaySchedule,
                    context.CurrentDaySchedule);

                var localDate = DateOnly.FromDateTime(localDateTime);
                var availabilityResponse = PublicBranchAvailabilityMapper.BuildResponse(
                    availability, evaluatedAtUtc, localDateTime, context.TimeZoneId, localDate);

                return b with { Availability = availabilityResponse };
            })
            .ToList();

        var enrichedResult = result with { Branches = branchesWithAvailability };

        return Result.Success(enrichedResult);
    }
}
