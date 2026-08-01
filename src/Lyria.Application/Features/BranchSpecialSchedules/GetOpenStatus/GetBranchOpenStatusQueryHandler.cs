using Lyria.Application.Abstractions.Messaging;
using Lyria.Application.Abstractions.Persistence;
using Lyria.Application.Abstractions.Services;
using Lyria.Application.Common.Results;
using Lyria.Application.Features.PublicCatalog;
using Lyria.Domain.Establishments.Branches;

namespace Lyria.Application.Features.BranchSpecialSchedules.GetOpenStatus;

public sealed class GetBranchOpenStatusQueryHandler(
    IBranchAvailabilityReadService availabilityReadService,
    ITimeZoneService timeZoneService,
    TimeProvider timeProvider)
    : IQueryHandler<GetBranchOpenStatusQuery, Result<BranchOpenStatusResponse>>
{
    public async ValueTask<Result<BranchOpenStatusResponse>> Handle(
        GetBranchOpenStatusQuery query,
        CancellationToken cancellationToken)
    {
        var branchId = new EstablishmentBranchId(query.BranchId);

        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        // We need to first get the context to know the timezone, then convert
        // Use a preliminary local date based on UTC; the service will use the correct one
        var preliminaryLocalDate = DateOnly.FromDateTime(utcNow.UtcDateTime);

        BranchAvailabilityContext? context =
            await availabilityReadService.GetAvailabilityContextAsync(
                branchId, preliminaryLocalDate, cancellationToken);

        if (context is null)
        {
            return Result.Failure<BranchOpenStatusResponse>(
                BranchSpecialScheduleErrors.BranchNotFound(query.BranchId));
        }

        if (!context.IsActive)
        {
            return Result.Failure<BranchOpenStatusResponse>(
                BranchSpecialScheduleErrors.BranchInactive(query.BranchId));
        }

        DateTime localDateTime = timeZoneService.ConvertUtcToLocal(utcNow, context.TimeZoneId);
        var localDate = DateOnly.FromDateTime(localDateTime);

        // If the local date differs from the preliminary one, re-fetch
        if (localDate != preliminaryLocalDate)
        {
            context = await availabilityReadService.GetAvailabilityContextAsync(
                branchId, localDate, cancellationToken);

            if (context is null)
            {
                return Result.Failure<BranchOpenStatusResponse>(
                    BranchSpecialScheduleErrors.BranchNotFound(query.BranchId));
            }
        }

        BranchAvailabilityResult availability = BranchAvailabilityCalculator.Calculate(
            localDateTime,
            context.PreviousDaySchedule,
            context.CurrentDaySchedule);

        var availabilityResponse = PublicBranchAvailabilityMapper.BuildResponse(
            availability, utcNow, localDateTime, context.TimeZoneId, localDate);

        var response = new BranchOpenStatusResponse(
            BranchId: context.BranchId,
            IsOpen: availabilityResponse.IsOpen,
            Status: availabilityResponse.Status,
            StatusName: availabilityResponse.StatusName,
            EvaluatedAtUtc: availabilityResponse.EvaluatedAtUtc,
            LocalDateTime: availabilityResponse.LocalDateTime,
            TimeZoneId: availabilityResponse.TimeZoneId,
            ScheduleSource: availabilityResponse.ScheduleSource,
            ScheduleDate: availabilityResponse.ScheduleDate,
            OpensAtLocal: availabilityResponse.OpensAtLocal,
            ClosesAtLocal: availabilityResponse.ClosesAtLocal,
            Reason: availabilityResponse.Reason);

        return Result.Success(response);
    }
}
