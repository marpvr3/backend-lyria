using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.PublicCatalog.GetBranchById;

public sealed class GetPublicBranchByIdQueryHandler(
    IPublicBranchReadService readService,
    IBranchAvailabilityReadService availabilityReadService,
    ITimeZoneService timeZoneService,
    TimeProvider timeProvider)
    : IQueryHandler<GetPublicBranchByIdQuery, Result<PublicBranchFullDetailResponse>>
{
    public async ValueTask<Result<PublicBranchFullDetailResponse>> Handle(
        GetPublicBranchByIdQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        var result = await readService.GetByIdAsync(branchId, cancellationToken);

        if (result is null)
        {
            return Result.Failure<PublicBranchFullDetailResponse>(
                PublicCatalogErrors.BranchNotFound(query.BranchId));
        }

        DateTimeOffset evaluatedAtUtc = timeProvider.GetUtcNow();

        var contexts = await availabilityReadService.GetAvailabilityContextsAsync(
            [branchId], evaluatedAtUtc, timeZoneService, cancellationToken);

        PublicBranchAvailabilityResponse availabilityResponse;

        if (contexts.TryGetValue(branchId, out var context))
        {
            DateTime localDateTime = timeZoneService.ConvertUtcToLocal(evaluatedAtUtc, context.TimeZoneId);
            var availability = BranchAvailabilityCalculator.Calculate(
                localDateTime,
                context.PreviousDaySchedule,
                context.CurrentDaySchedule);

            var localDate = DateOnly.FromDateTime(localDateTime);
            availabilityResponse = PublicBranchAvailabilityMapper.BuildResponse(
                availability, evaluatedAtUtc, localDateTime, context.TimeZoneId, localDate);
        }
        else
        {
            availabilityResponse = PublicBranchAvailabilityResponse.CreateNoSchedule(evaluatedAtUtc);
        }

        result = result with
        {
            Branch = result.Branch with { Availability = availabilityResponse }
        };

        return Result.Success(result);
    }
}
